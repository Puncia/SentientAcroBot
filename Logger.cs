using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SentientAcroBot
{
    public static class Logger
    {
        public static void Info(string msg, bool newLine = true, bool displayTime = true, ConsoleColor color = ConsoleColor.DarkCyan)
        {
            if (displayTime)
            {
                Console.ForegroundColor = color;
                Console.Write(DateTime.Now.ToLongTimeString() + " ");
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(msg + (newLine ? "\n" : ""));
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static void Error(string msg, bool newLine = true, bool displayTime = true, ConsoleColor color = ConsoleColor.Red)
        {
            if (displayTime)
            {
                Console.ForegroundColor = color;
                Console.Write(DateTime.Now.ToLongTimeString() + " ");
            }
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(msg + (newLine ? "\n" : ""));
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
