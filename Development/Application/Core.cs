using Nini.Config;
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
        INCOMING
    }

    [Script("core")]
    public class Core {

        public static Dictionary<Guid, NetworkBot> Networks;

        public static DateTime ConfigLastUpdate;

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

        internal static void DoStartup() {
            try {
                Core.ConfigLastUpdate = DateTime.Now;
                Core.SystemConfig = new IniConfigSource(Environment.CurrentDirectory + "/suibhne.ini");
                Core.SystemConfig.CaseSensitive = false;

                
                Core.Networks = new Dictionary<Guid, NetworkBot>();

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

                Core.Log("Database connection opened. Validating..");
                ValidateDatabase();
            }

            catch (Exception) {
                return;
            }

            Scripting.Scripting.GatherScriptNodes();

            try {
                String networkRootDirectory = Core.SystemConfig.Configs["Directories"].GetString("NetworkRootDirectory", Environment.CurrentDirectory + "/Configuration/Networks/");
                String[] networkDirectories = Directory.GetDirectories(networkRootDirectory);

                foreach (String networkDirectory in networkDirectories) {


                    NetworkBot network = new NetworkBot(networkDirectory);
                    Core.Networks.Add(network.Identifier, network);

                    if (File.Exists(networkDirectory + "/disabled"))
                        continue;

                    if (network.Status == Ostenvighx.Suibhne.Networks.Base.Reference.ConnectionStatus.Disconnected) {
                        network.Connect();
                    }
                }
            }

            catch (FileNotFoundException fnfe) {
                Console.WriteLine("Network configuration file not found: " + fnfe.Message);
            }

            catch (Exception e) {
                Console.WriteLine("Exception thrown: " + e);
            }

            ExtensionSystem.Instance.GetActiveExtensions();
        }

        public static void ValidateDatabase() {

        }

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
                    Console.ForegroundColor = ConsoleColor.Magenta;
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
