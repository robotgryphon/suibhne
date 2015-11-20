﻿using Nini.Config;
using Ostenvighx.Suibhne.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using System.Data;
using Newtonsoft.Json;
using System.Data.SQLite;

namespace Ostenvighx.Suibhne {

    public enum LogType : byte {
        GENERAL,
        ERROR,
        EXTENSIONS,
        OUTGOING,
        INCOMING,

        DEBUG
    }

    [Script("core")]
    public class Core {

        public static Dictionary<Guid, NetworkBot> Networks;

        public static DateTime ConfigLastUpdate;

        public static Boolean DEBUG;

        /// <summary>
        /// Base configuration directory. Will always have a trailing slash. ALWAYS.
        /// </summary>
        public static String ConfigDirectory;

        public static IniConfigSource SystemConfig;
        public static SQLiteConnection Database;

        [Script("startTime")]
        public static DateTime StartTime = DateTime.Now;

        public delegate void CoreLogEvent(String log, LogType type = LogType.GENERAL);
        public static event CoreLogEvent OnLogMessage;

        public static Version SystemVersion {
            get {

                Type r = typeof(Core);
                return r.Assembly.GetName().Version;
            }

            protected set { }
        }

        public static void LoadConfiguration() {
            try {
                Core.ConfigLastUpdate = DateTime.Now;
                Core.SystemConfig = new IniConfigSource(Environment.CurrentDirectory + "/suibhne.ini");
                Core.SystemConfig.CaseSensitive = false;

                Core.SystemConfig.ExpandKeyValues();
                Core.ConfigDirectory = Core.SystemConfig.Configs["Directories"].GetString("ConfigurationRoot", Environment.CurrentDirectory + "/Configuration/");
                if (Core.ConfigDirectory[Core.ConfigDirectory.Length - 1] != '/') {
                    Core.ConfigDirectory += "/";
                    Core.SystemConfig.Configs["Directories"].Set("ConfigurationRoot", Core.ConfigDirectory);
                    Core.SystemConfig.Save();
                }
                if (!File.Exists(Core.ConfigDirectory + "system.sns")) {
                    SQLiteConnection.CreateFile(Core.ConfigDirectory + "system.sns");
                }

                Core.Database = new SQLiteConnection("Data Source=" + Core.ConfigDirectory + "/system.sns");
                ExtensionSystem.Database = new SQLiteConnection("Data Source=" + Core.ConfigDirectory + "/extensions.sns");

                if(Core.SystemConfig.Configs["System"] != null)
                    Core.DEBUG = Core.SystemConfig.Configs["System"].GetBoolean("DEBUG_MODE", false);
            }

            catch (Exception) {

            }
        }
        public static void LoadNetworks() {
            Core.Networks = new Dictionary<Guid, NetworkBot>();

            String[] networkDirectories = Directory.GetDirectories(Core.ConfigDirectory + "/Networks/");

            foreach (String networkDirectory in networkDirectories) {
                try {
                    NetworkBot network = new NetworkBot(networkDirectory);
                    Core.Networks.Add(network.Identifier, network);
                }

                catch (FileNotFoundException fnfe) {
                    Console.WriteLine("Network configuration file not found: " + fnfe.Message);
                }

                catch (KeyNotFoundException) {
                    // Network unable to process correctly
                }

                catch (Exception e) {
                    Console.WriteLine("Exception thrown: " + e);
                }
            }
        }

        public static void StartNetworks() {
            foreach (NetworkBot network in Core.Networks.Values) {
                if (File.Exists(Core.ConfigDirectory + "/Networks/" + network.Identifier + "/disabled"))
                    continue;

                network.Connect();
            }
        }

        public static void Log(string message, LogType type = LogType.GENERAL) {

            // If we are debugging but not in debug mode, exit
            if (type == LogType.DEBUG && !DEBUG)
                return;

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
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    break;

                case LogType.DEBUG:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;

                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }

            Console.WriteLine("{0}", message);
            Console.ResetColor();

            if (OnLogMessage != null) OnLogMessage("[" + DateTime.Now + "] " + message, type);
        }
    }
}
