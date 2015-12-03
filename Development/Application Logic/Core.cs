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
using System.Diagnostics;

namespace Ostenvighx.Suibhne {

    public enum LogType : byte {
        GENERAL,
        ERROR,
        EXTENSIONS,
        OUTGOING,
        INCOMING,

        DEBUG
    }

    public class Core {

        public static Dictionary<Guid, NetworkBot> Networks;

        public static Boolean DEBUG;

        public enum Side : byte {
            CONNECTOR,
            INTERNAL,
            EXTENSION
        }

        /// <summary>
        /// Base configuration directory. Will always have a trailing slash. ALWAYS.
        /// </summary>
        public static String ConfigDirectory;

        internal static IniConfigSource SystemConfig;
        internal static SQLiteConnection Database;

        public static DateTime StartTime {
            get; private set;
        } = DateTime.Now;


        public delegate void CoreLogEvent(String log, LogType type = LogType.GENERAL);
        public static event CoreLogEvent OnLogMessage;

        public static Version SystemVersion {
            get {

                Type r = typeof(Core);
                return r.Assembly.GetName().Version;
            }

            protected set { }
        }

        public static void Initialize() {
            LoadConfiguration();

            Events.EventManager.Initialize();

            LoadNetworks();
            Commands.CommandManager.Initialize();

            Extensions.ExtensionSystem.Initialize();

            Commands.CommandManager.MapCommands();
        }

        private static void LoadConfiguration() {
            Core.Log("Loading the configuration data...");

            try {
                SystemConfig = new IniConfigSource(Environment.CurrentDirectory + "/suibhne.ini");
                SystemConfig.CaseSensitive = false;

                // Make sure directories config exists - if not, create it and add in the configroot
                if (SystemConfig.Configs["System"] == null) {
                    SystemConfig.AddConfig("System");
                    SystemConfig.Configs["System"].Set("ConfigurationRoot", Environment.CurrentDirectory + "/Configuration/");
                    SystemConfig.Save();
                }

                Core.ConfigDirectory = SystemConfig.Configs["System"].GetString("ConfigurationRoot", Environment.CurrentDirectory + "/Configuration/");

                // Make sure there's a trailing slash at the end of the config directory
                if (Core.ConfigDirectory[Core.ConfigDirectory.Length - 1] != '/') {
                    Core.ConfigDirectory += "/";
                    SystemConfig.Configs["System"].Set("ConfigurationRoot", Core.ConfigDirectory);
                    SystemConfig.Save();
                }

                SystemConfig.ExpandKeyValues();
                if (!File.Exists(Core.ConfigDirectory + "system.sns")) {
                    SQLiteConnection.CreateFile(Core.ConfigDirectory + "system.sns");
                }

                Database = new SQLiteConnection("Data Source=" + Core.ConfigDirectory + "/system.sns");

                DEBUG = SystemConfig.Configs["System"].GetBoolean("DEBUG_MODE", false);

                try {
                    Database.Open();
                    Core.Log("Database opened successfully.");
                    Database.Close();
                }

                catch (Exception dbe) {
                    Console.WriteLine(dbe);
                }

                CheckDatabase();
            }

            catch (Exception ex) {
                Core.Log(ex.Message);
                Core.Log(ex.StackTrace);
            }
        }

        private static void LoadNetworks() {
            Core.Networks = new Dictionary<Guid, NetworkBot>();

            Guid[] networks = LocationManager.GetNetworks();
            foreach (Guid netID in networks) {
                try {
                    NetworkBot network = new NetworkBot(netID);
                    Core.Networks.Add(netID, network);
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

        public static void Start() {
            foreach (NetworkBot network in Core.Networks.Values) {
                if (File.Exists(Core.ConfigDirectory + "/Networks/" + network.Identifier + "/disabled"))
                    continue;

                network.Connect();
            }
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

        /// <summary>
        /// Verifies the integrity of the system database.
        /// Checks tables are valid and existing.
        /// </summary>
        private static void CheckDatabase() {
            if (Core.Database.State != ConnectionState.Open)
                Core.Database.Open();

            try {
                SQLiteCommand tableCheck = Core.Database.CreateCommand();
                tableCheck.CommandText = "select count(*) from sqlite_master WHERE type='table' AND (name='Identifiers' OR name='Commands');";
                int i = int.Parse(tableCheck.ExecuteScalar().ToString());

                if (i >= 2) return;

                tableCheck.CommandText = "create table if not exists Identifiers (" +
                    "`Identifier`	TEXT NOT NULL UNIQUE," +
	                "`ParentId`	TEXT," +
	                "`Name`	TEXT," +
	                "`LocationType`	INTEGER," +
                    "PRIMARY KEY(Identifier)," +
                    "FOREIGN KEY(`ParentId`) REFERENCES Identifier" +
                  ")";

                if (tableCheck.ExecuteNonQuery() == 1)
                    Core.Log("Created identifiers table.");

                tableCheck.CommandText = "create table if not exists Commands (" +
                    "`Command`	TEXT UNIQUE," +
                    "`Extension`	TEXT," +
                    "`Handler`	TEXT," +
                    "`DefaultAccess`	INTEGER DEFAULT 1," +
                    "PRIMARY KEY(Command)" +
                  ")";

                if (tableCheck.ExecuteNonQuery() == 1)
                    Core.Log("Created command-linking table.");
            }

            catch(Exception ex) {

            }
            finally { Core.Database.Close(); }
        }
    }
}
