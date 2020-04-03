using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace SentientAcroBot
{
    /*https://discordapp.com/oauth2/authorize?client_id=CLIENT_ID&scope=bot&permissions=142336*/
    public class AcroBot
    {
        private DiscordSocketClient socketClient;
        List<Acro> acroInstances;


        private string token = "";
        private const string token_file = "token.key";

        static void Main(string[] args)
            => new AcroBot().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            if (File.Exists(token_file))
            {
                token = File.ReadAllText(token_file);
            }
            else
            {
                Logger.Error("Invalid Discord token");
                System.Console.ReadLine();
                return;
            }

            socketClient = new DiscordSocketClient();
            acroInstances = new List<Acro>();

            socketClient.Log += Log;
            socketClient.Connected += SocketClient_Connected;
            socketClient.MessageReceived += HandleCommandAsync;

            await socketClient.LoginAsync(TokenType.Bot, token);
            await socketClient.StartAsync();

            //Blocks this task until the program is closed
            await Task.Delay(-1);
        }

        private async Task HandleCommandAsync(SocketMessage arg)
        {
            var message = arg as SocketUserMessage;

            if (message is null || message.Author.IsBot)
                return;

            if (message.Content == ".acro")
            {
                if (IsAcroRunning((message.Author as SocketGuildUser).Guild))
                {
                    await (arg.Channel as ISocketMessageChannel).SendMessageAsync("acro already running!!!");
                    return;
                }
                Logger.Info("Acro initiated (" + (message.Author as SocketGuildUser).Guild.Name + ")");


                Acro instance = new Acro((arg.Channel as SocketGuildChannel).Guild, (arg.Channel as SocketChannel));

                acroInstances.Add(instance);
                instance.StartSubmissionTimer();
                //instance.SubmissionTimerElapsed += Instance_SubmissionTimerElapsed;
                instance.VoteTimerElapsed += Instance_VoteTimerElapsed;
            }
            //controllo se il messaggio è una potenziale acro submission per questo server o un voto
            else if (acroInstances.Count > 0)
            {                
                for (int i = 0; i < acroInstances.Count; i++)
                {
                    if (acroInstances[i].GetChannel().Id == message.Channel.Id)
                    {
                        if (!acroInstances[i].IsVoteTimerEnabled() && acroInstances[i].IsSubmissionTimerEnabled() && acroInstances[i].RegisterSubmission(arg.Content, arg.Author))
                        {
                            Logger.Info(arg.Author + " submitted");
                            await arg.DeleteAsync();
                        }
                        else if (!acroInstances[i].IsSubmissionTimerEnabled() && acroInstances[i].RegisterVote(arg.Content, arg.Author))
                        {
                            Logger.Info(arg.Author + " voted");
                            await arg.DeleteAsync();
                        }
                        else
                        {
                            
                        }    
                    }
                }
            }
        }

        private void Instance_VoteTimerElapsed(Acro acroInstance)
        {
            acroInstances.Remove(acroInstance);
        }

        private void Instance_SubmissionTimerElapsed(Acro acroInstance)
        {
            //acroInstances.Remove(acroInstance);
        }

        private async Task SocketClient_Connected()
        {
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private bool IsAcroRunning(SocketGuild guild)
        {
            if (acroInstances == null || acroInstances.Count < 1)
                return false;

            foreach (Acro inst in acroInstances)
            {
                if ((inst.GetGuild().Id == guild.Id) && (inst.IsSubmissionTimerEnabled() || inst.IsVoteTimerEnabled()))
                    return true;
            }

            return false;
        }
    }

}
