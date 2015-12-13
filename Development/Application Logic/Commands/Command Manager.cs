using Newtonsoft.Json.Linq;
using Nini.Config;
using Ostenvighx.Suibhne.Services.Chat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Ostenvighx.Suibhne.Extensions;
using System.Data;
using System.Data.SQLite;
using System.Text.RegularExpressions;
using Ostenvighx.Suibhne.System_Commands;
using System.Diagnostics;

namespace Ostenvighx.Suibhne.Commands {
    public class CommandManager {

        protected Dictionary<string, CommandMap> CommandMapping;
        private static CommandManager instance;

        public static CommandManager Instance {
            get {
                if (instance == null)
                    instance = new CommandManager();
                
                return instance;
            }
        }

        private CommandManager() {
            this.CommandMapping = new Dictionary<string, CommandMap>();
        }

        public static void Initialize() {
            if (instance == null)
                instance = new CommandManager();
        }

        public void WipeCommandMap() {
            this.CommandMapping.Clear();
        }

        public bool RegisterCommand(String command, CommandMap map) {
            if (!this.CommandMapping.ContainsKey(command.ToLower())) {
                CommandMapping.Add(command.ToLower(), map);
                return true;
            }
            return false;
        }

        public static int MapCommands() {
            int mappedCommands = 0;
            if (Core.SystemConfig == null)
                return 0;

            
            try {
                if(Core.Database.State != ConnectionState.Open)
                    Core.Database.Open();

                instance.WipeCommandMap();

                DataTable results = new DataTable();
                SQLiteCommand fetch_mappings = Core.Database.CreateCommand();
                fetch_mappings.CommandText = "SELECT * FROM Commands;";
                SQLiteDataReader sdr = fetch_mappings.ExecuteReader();
                results.Load(sdr);

                // Loop through requested commands (!<command>)
                foreach (DataRow commandEntry in results.Rows) {

                    if(Core.DEBUG)
                        Debug.WriteLine(">>> Attempting to map command " + commandEntry["Command"] + " to extension " + commandEntry["Extension"] + " (handler: " + commandEntry["Handler"] + ")", "Extensions");

                    CommandMap cm = new CommandMap();
                    cm.Handler = commandEntry["Handler"].ToString();
                    cm.Extension = Guid.Parse(commandEntry["Extension"].ToString());

                    if (commandEntry["DefaultAccess"].ToString() == "")
                        cm.AccessLevel = 1;
                    else
                        cm.AccessLevel = (byte) int.Parse(commandEntry["DefaultAccess"].ToString());

                    instance.RegisterCommand((string) commandEntry["Command"], cm);
                    mappedCommands++;
                }
                
            }

            catch (Exception) {

            }

            finally {
                Core.Database.Close();
            }

            instance.RegisterCommand("commands", new CommandMap() { AccessLevel = 1, Is_Internal = true });
            instance.RegisterCommand("help", new CommandMap() { AccessLevel = 1, Is_Internal = true });
            return mappedCommands;
        }

        internal static CommandMap Lookup(string commandName) {
            if (instance.CommandMapping.ContainsKey(commandName))
                return instance.CommandMapping[commandName];

            throw new KeyNotFoundException("That command is not registered.");
        }

        internal String[] GetAvailableCommandsForUser(User u, bool includeAccessLevels = false) {
            List<String> available = new List<String>();

            foreach (KeyValuePair<String, CommandMap> cm in CommandManager.Instance.CommandMapping) {
                if (ExtensionSystem.Instance.Extensions.ContainsKey(cm.Value.Extension)) {
                    if (ExtensionSystem.Instance.Extensions[cm.Value.Extension].Ready)
                        if(cm.Value.AccessLevel <= u.NetworkAuthLevel)
                            available.Add(cm.Key + (includeAccessLevels ? " (" + cm.Value.AccessLevel + ")" : ""));
                } else {
                    // Command is hard-coded into here
                    if (cm.Value.AccessLevel <= u.NetworkAuthLevel)
                        available.Add(cm.Key + (includeAccessLevels ? " (" + cm.Value.AccessLevel + ")" : ""));
                }
            }

            available.Sort();
            return available.ToArray();
        }

        internal static void RouteInternalCommand(string command, JObject ev) {
            Guid location = ev["location"]["id"].ToObject<Guid>();
            String args = ev["arguments"].ToString();

            Core.Log("REcieved internal command " + command + " from " + location + ", with " + 
                (args != "" ? "arguments " + args : "no arguments") +
                ".");

        }
    }
}
