﻿using Ostenvighx.Api.Irc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ostenvighx.Suibhne {

    public enum LogType : byte {
        GENERAL,
        ERROR,
        EXTENSIONS,
        OUTGOING,
        INCOMING
    }

    class Core {

        public static Dictionary<Guid, Guid> NetworkLocationMap = new Dictionary<Guid, Guid>();

        public static void Log(string message, LogType type = LogType.GENERAL) {

            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("[{0}] ", DateTime.Now);

            switch (type) {
                case LogType.GENERAL:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    break;

                case LogType.ERROR:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;

                case LogType.INCOMING:
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    break;

                case LogType.OUTGOING:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;

                case LogType.EXTENSIONS:
                    Console.ForegroundColor = ConsoleColor.DarkMagenta;
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }

            Console.WriteLine("{0}", message);
            Console.ResetColor();
        }
    }
}
