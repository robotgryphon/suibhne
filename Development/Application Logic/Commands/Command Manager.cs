﻿using Newtonsoft.Json.Linq;
using Nini.Config;
using Ostenvighx.Suibhne.Networks.Base;
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

                    Core.Log(">>> Attempting to map command " + commandEntry["Command"] + " to extension " + commandEntry["Extension"] + " (handler: " + commandEntry["Handler"] + ")", LogType.DEBUG);

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

        public void HandleCommand(NetworkBot conn, Message message) {
            message.message = message.message.Trim();
            String[] messageParts = message.message.Split(new char[] { ' ' });
            String command = messageParts[0].ToLower().TrimStart(new char[] { '!' }).TrimEnd();
            String subCommand = "";
            if (messageParts.Length > 1)
                subCommand = messageParts[1].ToLower();

            Message response = new Message(message.locationID, conn.Me, "Response");
            response.type = Suibhne.Networks.Base.Reference.MessageType.PublicMessage;
            if (message.type != Networks.Base.Reference.MessageType.PublicAction || message.type != Networks.Base.Reference.MessageType.PublicMessage) {
                response.target = message.target;
            }

            if(!Regex.Match(command, @"[\w\d]+").Success)
                return;
            
            if (!CommandMapping.ContainsKey(command)) {
                response.type = Suibhne.Networks.Base.Reference.MessageType.PublicAction;
                response.message = "is not sure what to do with this information. [INVALID COMMAND]";
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
                            Core.Log("Recieved help command for command '" + subCommand + "'. Telling extension " + ext.Name + " to handle it. [handler: " + mappedCommand.Handler + "]", LogType.EXTENSIONS);
                            ext.HandleHelpCommandRecieved(conn, mappedCommand, message);
                        } else {
                            response.type = Suibhne.Networks.Base.Reference.MessageType.PublicAction;
                            response.message = "does not have information on that command.";
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

                    Core.Log("Recieved command '" + command + "'. Telling extension " + extension.Name + " to handle it. [handler: " + cmd.Handler + "]", LogType.EXTENSIONS);
                    if (!extension.Ready) {
                        response.message = "I have {" + command + "} registered as a command, but it looks like the extension isn't ready yet. Try again later.";
                        conn.SendMessage(response);
                    } else {
                        extension.HandleCommandRecieved(conn, cmd, message);
                    }

                    break;
            }
        }

        private void HandleSystemCommand(NetworkBot conn, Message msg) {
            string[] messageParts = msg.message.Split(' ');
            String subCommand = "";
            Message response = new Message(msg.locationID, conn.Me, "System Command Response");
            if (msg.type != Networks.Base.Reference.MessageType.PublicAction || msg.type != Networks.Base.Reference.MessageType.PublicMessage) {
                response.target = msg.target;
            }

            if (messageParts.Length > 1)
                subCommand = messageParts[1];

            if (messageParts.Length > 1 && subCommand != "") {
                switch (subCommand) {
                    case "exts":
                    case "extensions":
                        SysCommands.Extensions(conn, msg);
                        break;

                    case "reload":
                        if (messageParts.Length < 3) {
                            response.message = "I need more parameters than that. Try config, access, or extensions.";
                            conn.SendMessage(response);
                            break;
                        }

                        switch (messageParts[2].ToLower()) {
                            case "config":
                                if (Core.SystemConfig != null) {
                                    if (Core.ConfigLastUpdate < File.GetLastWriteTime(Core.SystemConfig.SavePath)) {
                                        try {
                                            Core.SystemConfig = new IniConfigSource(Environment.CurrentDirectory + @"/suibhne.ini");
                                            int numRemapped = MapCommands();
                                            response.message = "Successfully remapped " + numRemapped + " commands to " + ExtensionSystem.Instance.Extensions.Count + " extensions.";
                                            conn.SendMessage(response);

                                            // TODO: MapAccessLevels();
                                            Core.ConfigLastUpdate = DateTime.Now;
                                        }

                                        catch (Exception e) {
                                            response.message = "Error processing request: " + e.Message;
                                            conn.SendMessage(response);
                                        }
                                    } else {
                                        response.message = "Your system config is up-to-date.";
                                        conn.SendMessage(response);
                                    }
                                }
                                break;

                            case "extensions":
                            case "access":
                                response.message = "whispers: \"This isn't quite available yet.\"";
                                response.type = Networks.Base.Reference.MessageType.PublicAction;
                                conn.SendMessage(response);
                                response.type = Networks.Base.Reference.MessageType.PublicMessage;
                                break;

                            default:
                                response.message = "is not sure what to do with that. Try config or extensions as a parameter.";
                                response.type = Networks.Base.Reference.MessageType.PublicAction;
                                conn.SendMessage(response);
                                response.type = Networks.Base.Reference.MessageType.PublicMessage;
                                break;
                        }
                        break;

                    case "version":
                        SysCommands.Version(conn, msg);
                        break;

                    case "netinfo":
                        SysCommands.NetworkInfo(conn, msg);
                        break;

                    case "uptime":
                        TimeSpan diff = DateTime.Now - Core.StartTime;
                        response.type = Ostenvighx.Suibhne.Networks.Base.Reference.MessageType.PublicAction;
                        response.message = "has been up for " +
                            (diff.Days > 0 ? diff.Days + " days" : "") +
                            (diff.Hours > 0 ? diff.Hours + " hours, " : "") +
                            (diff.Minutes > 0 ? diff.Minutes + " minutes, " : "") +
                            (diff.Seconds > 0 ? diff.Seconds + " seconds" : "") + ". [Up since " + Core.StartTime.ToString() + "]";

                        conn.SendMessage(response);
                        response.type = Ostenvighx.Suibhne.Networks.Base.Reference.MessageType.PublicMessage;
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
                        response.type = Suibhne.Networks.Base.Reference.MessageType.PublicAction;
                        response.message = "does not know what you are asking for. "; // + "[Invalid subcommand]", Formatter.Colors.Orange);
                        conn.SendMessage(response);
                        response.type = Ostenvighx.Suibhne.Networks.Base.Reference.MessageType.PublicMessage;
                        break;
                }
            } else {
                response.message = "Available system commands: {exts/extensions, reload, id/identifier, version, netinfo, uptime}";
                conn.SendMessage(response);
            }
        }

    }
}
