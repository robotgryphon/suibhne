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

                    Debug.WriteLine(">>> Attempting to map command " + commandEntry["Command"] + " to extension " + commandEntry["Extension"] + " (handler: " + commandEntry["Handler"] + ")", "Extensions");

                    CommandMap cm = new CommandMap();
                    cm.Handler = commandEntry["Handler"].ToString();
                    cm.Extension = Guid.Parse(commandEntry["Extension"].ToString());
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

            CommandMap sys = new CommandMap();
            sys.Handler = "system";
            sys.Extension = Guid.Empty;

            // TODO: Implement the access levels in the database and modify the following lines
            // sys.AccessLevel = (byte) Core.SystemConfig.Configs["CommandAccess"].GetInt("sys", 250);
            sys.AccessLevel = 250;
            instance.RegisterCommand("sys", sys);

            // sys.AccessLevel = (byte) Core.SystemConfig.Configs["CommandAccess"].GetInt("system", 250);
            instance.RegisterCommand("system", sys);
            instance.RegisterCommand("commands", new CommandMap() { AccessLevel = 1 });
            instance.RegisterCommand("help", new CommandMap() { AccessLevel = 1 });
            return mappedCommands;
        }

        public String[] GetAvailableCommandsForUser(User u, bool includeAccessLevels = false) {
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

        public void HandleCommand(ServiceWrapper conn, Message message) {
            message.message = message.message.Trim();
            String[] messageParts = message.message.Split(new char[] { ' ' });
            String command = messageParts[0].ToLower().TrimStart(new char[] { '!' }).TrimEnd();
            String subCommand = "";
            if (messageParts.Length > 1)
                subCommand = messageParts[1].ToLower();

            Message response = new Message(message.locationID, conn.Me, "Response");
            if (message.IsPrivate) response.target = message.target;

            if(!Regex.Match(command, @"[\w\d]+").Success)
                return;
            
            if (!CommandMapping.ContainsKey(command)) {
                response.message = "I am not sure what to do with this information. [Invalid command]";
                conn.SendMessage(response);
                return;
            }

            CommandMap cmd = CommandMapping[command];
            if (!(cmd.AccessLevel <= message.sender.NetworkAuthLevel)) {
                response.message = "You do not have permission to run this command.";
                conn.SendMessage(response);
                return;
            }

            switch (command) {
                case "commands":
                    SysCommands.Commands(conn, message);
                    break;

                case "sys":
                case "system":
                    HandleSystemCommand(conn, message);
                    break;

                case "help":
                    if (subCommand != "") {
                        // Map command to id
                        if (CommandMapping.ContainsKey(subCommand)) {
                            CommandMap mappedCommand = CommandMapping[subCommand];
                            ExtensionMap ext = ExtensionSystem.Instance.Extensions[mappedCommand.Extension];
                            Debug.WriteLine("Recieved help command for command '" + subCommand + "'. Telling extension " + ext.Name + " to handle it. [handler: " + mappedCommand.Handler + "]", LogType.EXTENSIONS);
                            ext.HandleHelpCommandRecieved(conn, mappedCommand, message);
                        } else {
                            response.message = "I do not have information on that command. Sorry!";
                            conn.SendMessage(response);
                        }
                    } else {
                        response.message = "The !help command is used to get information on a command. Format is !help <command>.";
                        conn.SendMessage(response);
                        break;
                    }
                    break;

                default:
                    ExtensionMap extension = ExtensionSystem.Instance.Extensions[CommandMapping[command].Extension];

                    Debug.WriteLine("Recieved command '" + command + "'. Telling extension " + extension.Name + " to handle it. [handler: " + cmd.Handler + "]", LogType.EXTENSIONS);
                    if (!extension.Ready) {
                        response.message = "I have {" + command + "} registered as a command, but it looks like the extension isn't ready yet. Try again later.";
                        conn.SendMessage(response);
                    } else {
                        extension.HandleCommandRecieved(conn, cmd, message);
                    }

                    break;
            }
        }

        private void HandleSystemCommand(ServiceWrapper conn, Message msg) {
            string[] messageParts = msg.message.Split(' ');
            String subCommand = "";
            Message response = new Message(msg.locationID, conn.Me, "System Command Response");
            if (msg.IsPrivate) response.target = msg.target;

            if (messageParts.Length > 1)
                subCommand = messageParts[1];

            if (messageParts.Length > 1 && subCommand != "") {
                switch (subCommand) {
                    case "exts":
                    case "extensions":
                        SysCommands.Extensions(conn, msg);
                        break;

                    case "version":
                        SysCommands.Version(conn, msg);
                        break;

                    case "netinfo":
                        SysCommands.NetworkInfo(conn, msg);
                        break;

                    case "uptime":
                        TimeSpan diff = DateTime.Now - Core.StartTime;
                        response.message = "The system has been up for " +
                            (diff.Days > 0 ? diff.Days + " days" : "") +
                            (diff.Hours > 0 ? diff.Hours + " hours, " : "") +
                            (diff.Minutes > 0 ? diff.Minutes + " minutes, " : "") +
                            (diff.Seconds > 0 ? diff.Seconds + " seconds" : "") + ". [Up since " + Core.StartTime.ToString() + "]";

                        conn.SendMessage(response);
                        break;

                    case "id":
                    case "identifier":
                        switch(messageParts.Length) {
                            case 2:
                                Location location = LocationManager.GetLocationInfo(msg.locationID);
                                
                                response.message = "Current identifier for location \"" + location.Name + "\": " + msg.locationID + ". Network identifier: " + conn.Identifier;
                                conn.SendMessage(response);
                                break;

                            default:

                                String param = msg.message.Split(new char[]{ ' ' }, 3)[2];

                                if (param.Contains(':')) {

                                    // Network and locaiton lookup
                                    String networkName = param.Split(':')[0].Trim();
                                    String locationName = param.Split(':')[1].Trim();

                                    KeyValuePair<Guid, Location> l = LocationManager.GetLocationInfo(networkName, locationName);
                                    response.message = "Current identifier for location " + l.Value.Name +
                                        " on network " + networkName + ": " + l.Key + ".";

                                    conn.SendMessage(response);

                                } else {
                                    
                                    // Local location lookup
                                    KeyValuePair<Guid, Location> l = LocationManager.GetLocationInfo(conn.Identifier, param);

                                    // If the location wasn't found, try to find it as a network instead
                                    if (l.Key == Guid.Empty) {
                                        l = LocationManager.GetLocationInfo(param, "");
                                        if (l.Key == Guid.Empty) {
                                            response.message = "Could not find information for that location. Make sure you spelled everythign correctly.";
                                            conn.SendMessage(response);

                                            break;
                                        }
                                    }

                                    response.message = "Current identifier for " + l.Value.Name + ": " + l.Key;
                                    conn.SendMessage(response);

                                }

                                break;
                        }
                        
                        break;

                    default:
                        response.message = "I do not know what you are asking for. ";
                        conn.SendMessage(response);
                        break;
                }
            } else {
                response.message = "Available system commands: {exts/extensions, reload, id/identifier, version, netinfo, uptime}";
                conn.SendMessage(response);
            }
        }

    }
}
