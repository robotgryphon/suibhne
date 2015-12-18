using Nini.Config;
using Ostenvighx.Suibhne.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Newtonsoft.Json;
using System.Diagnostics;
using System.Data;
using System.Data.SQLite;
using Ostenvighx.Suibhne.Configuration;
using Ostenvighx.Suibhne.Services;

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

        public static Dictionary<Guid, Services.ServiceConnector> ConnectedServices;

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
        internal static String CommandPrefix;

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
            ConfigHelper.LoadConfiguration();

            Events.EventManager.Initialize();

            LoadServiceInformation();
            Commands.CommandManager.Initialize();

            Extensions.ExtensionSystem.Initialize();

            Commands.CommandManager.MapCommands();
        }

        private static void LoadServiceInformation() {
            Core.ConnectedServices = new Dictionary<Guid, Services.ServiceConnector>();

            Guid[] serviceIDs = ServiceManager.GetServiceIdentifiers();
            foreach (Guid serviceID in serviceIDs) {
                try {
                    ServiceConnector serv = Services.ServiceLoader.GenerateConnector(serviceID);
                    if(serv != null)
                        Core.ConnectedServices.Add(serviceID, serv);
                }

                catch (FileNotFoundException fnf) {
                    Log(fnf.Message, LogType.ERROR);
                }

                catch (Exception e) {
                    Console.WriteLine("Exception thrown: " + e);
                }
            }
        }

        public static void Start() {
            foreach (ServiceConnector serv in Core.ConnectedServices.Values) {
                // If the service is disabled by a flag file, do not start it- just keep swimmin'
                if (File.Exists(Core.ConfigDirectory + "/Services/" + serv.Identifier + "/disabled"))
                    continue;

                serv.Start();
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

            StackFrame sf = new StackTrace().GetFrame(1);

            Console.WriteLine("{0}{1}", type == LogType.ERROR ? "[" + sf.GetMethod().DeclaringType.Name + "." + sf.GetMethod().Name + "] " : "", message);
            Console.ResetColor();

            if (OnLogMessage != null) OnLogMessage("[" + DateTime.Now + "] " + message, type);
        }
    }
}
