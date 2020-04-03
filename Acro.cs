using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace SentientAcroBot
{
    public class Acro
    {
        SocketGuild Guild;
        SocketChannel Channel;
        private Timer SubmissionTimer;
        private Timer VoteTimer;
        private string Acronym = "";

        public struct Submission
        {
            public Submission(SocketUser user, string text, SocketUser votingUser = null, int votes = 0)
            {
                User = user;
                Text = text;
                Votes = votes;
                VotingUsers = new List<SocketUser>();
                if (votingUser != null)
                    VotingUsers.Add(votingUser);

                
            }
            public SocketUser User { get; set; }
            public string Text { get; set; }
            public int Votes { get; set; }
            public List<SocketUser> VotingUsers { get; set; }
        };

        private List<Submission> Submissions;

        public delegate void ElapsedEventHandler(Acro sender);
        public event ElapsedEventHandler SubmissionTimerElapsed;
        public event ElapsedEventHandler VoteTimerElapsed;

        int AcroTimer_ms = 15000;
        int VoteTimer_ms = 15000;
        
        int VoteTimer_PerSubmission = 2000;

        readonly int minLength = 2;
        readonly int maxLength = 7;
        readonly List<char> forbiddenChars;

        public Acro(SocketGuild guild, SocketChannel channel)
        {
            Guild = guild;
            Channel = channel;

            Submissions = new List<Submission> ();

            SubmissionTimer = new Timer();
            VoteTimer = new Timer();

            SubmissionTimer.Elapsed += SubmissionTimer_Elapsed;
            VoteTimer.Elapsed += VoteTimer_Elapsed;

            forbiddenChars = new List<char>()
            {
                'X', 'Y', 'J', 'K', 'W', 'Z'
            };
        }
        
        public void StartSubmissionTimer()
        {
            SendMessage(GetAcronymBuilder(BuildAcronym(), AcroTimer_ms / 1000 + "s rimanenti.").Build());
            SubmissionTimer.Interval = AcroTimer_ms;
            SubmissionTimer.Start();
            SubmissionTimer.AutoReset = false;
        }

        public void StartVoteTimer()
        {
            VoteTimer.Interval = VoteTimer_ms;
            VoteTimer.Start();
            VoteTimer.AutoReset = false;
        }

        private EmbedBuilder GetAcronymBuilder(string Acronym, string Footer)
        {
            EmbedBuilder builder = new EmbedBuilder();
            builder.Color = Color.Green;
            builder.Title = "Acro";
            builder.Description = "**" + Acronym + "**";
            builder.WithFooter(Footer);

            return builder;
        }

        private EmbedBuilder GetSubmissionsBuilder(List<Submission> submissions)
        {
            EmbedBuilder builder = new EmbedBuilder();
            string part = "partecipanti: ";
            List<SocketUser> partecipanti = submissions.Select(x => x.User).ToList();
            Shuffle(submissions);
            Shuffle(partecipanti);
            VoteTimer_ms += submissions.Count * VoteTimer_PerSubmission;

            builder.Color = new Color(255, 105, 180);
            builder.Title = Submissions.Count + " submissions:";

            builder.Description = "**";
            for (int i = 0; i < Submissions.Count; i++)
            {
                builder.Description += "``" + (i + 1).ToString() + "``" + ". " + Submissions.ElementAt(i).Text + "\n";
            }
            builder.Description += "**";

            foreach (SocketUser u in partecipanti)
            {
                part += (u as SocketGuildUser).Nickname != null ? (u as SocketGuildUser).Nickname + ", " : u.Username + ", ";
            }

            part = part.Substring(0, part.Length - 2);
            builder.WithFooter(part);

            return builder;
        }

        public void Shuffle<T>(IList<T> list)
        {
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            int n = list.Count;
            while (n > 1)
            {
                byte[] box = new byte[1];
                do provider.GetBytes(box);
                while (!(box[0] < n * (Byte.MaxValue / n)));
                int k = (box[0] % n);
                n--;
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        private void SubmissionTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Logger.Info("Submission timer elapsed");
            SubmissionTimerElapsed?.Invoke(this);
            
            string SubmissionsMessage = "";

            if (Submissions.Count > 0)
            {
                if(Submissions.Count == 1)
                {
                    VoteTimer_Elapsed(sender, e);
                    return;
                }
                foreach (Submission sub in Submissions)
                {
                    SubmissionsMessage += sub.Text + "\n";
                }

                SendMessage(GetSubmissionsBuilder(Submissions).Build());
                SendMessage(new EmbedBuilder().WithFooter(VoteTimer_ms / 1000 + "s rimanenti per sguazzare").Build());

                StartVoteTimer();
            }
            else if(Submissions.Count == 0)
            {
                SendMessage("No valid submissions");
            }
        }

        private void VoteTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            List<Submission> s_submissions;
            Logger.Info("Vote timer elapsed");
            VoteTimerElapsed?.Invoke(this);

            EmbedBuilder builder = new EmbedBuilder();
            string winning_acros = "";
            builder.WithTitle("Winning acros");
            s_submissions = GetWinner();

            int max_votes = s_submissions.First().Votes;
            foreach(Submission sub in s_submissions)
            {
                if (sub.Votes == max_votes)
                    winning_acros += "• **" + sub.Text + "** | by " + ((sub.User as SocketGuildUser).Nickname != null ? (sub.User as SocketGuildUser).Nickname : sub.User.Username) + " (" + sub.Votes + " vote" + (sub.Votes != 1 ? "s" : "") + ")" + "\n";
                else
                    break;
            }
            builder.WithDescription(winning_acros);
            builder.WithColor(230, 126, 34);
            SendMessage(builder.Build());
        }

        private List<Submission> GetWinner()
        {
            return Submissions.OrderByDescending(x => x.Votes).ToList();
        }

        public SocketGuild GetGuild()
        {
            return Guild;
        }

        public SocketChannel GetChannel()
        {
            return Channel;
        }

        public void SendMessage(Embed message)
        {
            (Channel as ISocketMessageChannel).SendMessageAsync(embed: message);
        }

        public void SendMessage(string message)
        {
            (Channel as ISocketMessageChannel).SendMessageAsync(message);
        }

        //public void SendMessage(Embed builder)
        //{
        //    (Channel as ISocketMessageChannel).SendMessageAsync("", false, builder);
        //}

        public string BuildAcronym()
        {
            Random rnd = new Random();
            int l = rnd.Next(minLength, maxLength + 1);

            char currentChar;
            for (int i = 0; i < l; i++)
            {
                do
                {
                    currentChar = (char)rnd.Next(65, 90 + 1);
                } while (forbiddenChars.Contains(currentChar) ||
                ((Acronym.Contains("Q") && currentChar == 'Q') ||
                (Acronym.Contains("H") && currentChar == 'H')) ||
                (Acronym.Length > 0 ? (Acronym.Replace(".", "")[Acronym.Replace(".", "").Length - 1] == currentChar) : false) ||
                ((i + 1 == l) && currentChar == 'H'));

                Acronym += currentChar + ".";
            }

            if(Acronym.Length / 2 > 5)
                AcroTimer_ms = 60000;
            else
                AcroTimer_ms += Convert.ToInt32(Math.Pow(Convert.ToDouble(2), Convert.ToDouble(Acronym.Length/2)) * 1000);
            
            return Acronym;
        }

        public bool RegisterSubmission(string message, SocketUser user)
        {
            string[] words = RemoveDiacritics(message).Split(' ');
            string[] acroLetter = Acronym.Split('.');

            int i = 0;

            if (!Submissions.Any(x => x.User.Username == user.Username) && (message.Count(s => s == ' ') == Acronym.Count(p => p == '.') - 1))
            {
                foreach (string word in words)
                {
                    if (word.ToUpper().Replace("(", "").Replace(")", "").Replace("\"", "").StartsWith(acroLetter[i].ToUpper()))
                    {
                        i++;
                    }
                }
                if (i == words.Length)
                {
                    Submissions.Add(new Submission(user, GetTitleCase(message)));
                    return true;
                }
            }
            return false;
        }

        public string GetTitleCase(string message)
        {
            TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;
            return textInfo.ToTitleCase(message);
        }

        public string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            text = text.Normalize(NormalizationForm.FormD);
            var chars = text.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
            return new string(chars).Normalize(NormalizationForm.FormC);
        }

        public bool RegisterVote(string vote, SocketUser user)
        {
            if(vote.All(char.IsDigit) &&
                (Convert.ToInt32(vote) <= Submissions.Count) &&
                user != Submissions.ElementAt(Convert.ToInt32(vote) - 1).User &&
                !Submissions.ElementAt(Convert.ToInt32(vote) - 1).VotingUsers.Contains(user))
            {
                Submissions[Convert.ToInt32(vote) - 1] = new Submission(Submissions[Convert.ToInt32(vote) - 1].User, Submissions[Convert.ToInt32(vote) - 1].Text, user, Submissions[Convert.ToInt32(vote) - 1].Votes + 1);

                return true;
            }
            return false;
        }

        public bool IsSubmissionTimerEnabled()
        {
            return SubmissionTimer.Enabled;
        }

        public bool IsVoteTimerEnabled()
        {
            return VoteTimer.Enabled;
        }
    }
}
