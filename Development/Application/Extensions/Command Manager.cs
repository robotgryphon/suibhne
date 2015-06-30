using Nini.Config;
using Ostenvighx.Suibhne.Networks.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.Extensions {
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

        public int MapCommands() {
            int mappedCommands = 0;
            if (Core.SystemConfig == null)
                return 0;

            WipeCommandMap();

            String[] commands = Core.SystemConfig.Configs["Commands"].GetKeys();
            foreach (String commandKey in commands) {
                String commandMap = Core.SystemConfig.Configs["Commands"].GetString(commandKey);
                try {
                    CommandMap c = new CommandMap();
                    c.CommandString = commandKey.ToLower();
                    String ExtensionsRootDirectory = Core.SystemConfig.Configs["Directories"].GetString("ExtensionsRootDirectory", Environment.CurrentDirectory + "/Extensions/");
                    int nameEnd = commandMap.IndexOf(":");
                    if (nameEnd == -1) nameEnd = 0;

                    c.AccessLevel = (byte) Core.SystemConfig.Configs["CommandAccess"].GetInt(c.CommandString, 1);

                    String extensionDirectory = ExtensionsRootDirectory + commandMap.Substring(0, nameEnd);
                    if (Directory.Exists(extensionDirectory)) {
                        ExtensionMap em = ExtensionLoader.LoadExtension(extensionDirectory);
                        if (em.Identifier != Guid.Empty) {
                            c.Extension = em.Identifier;
                            Guid methodID = ExtensionLoader.GetMethodIdentifier(extensionDirectory, commandMap.Substring(nameEnd + 1).Trim());
                            if (methodID != Guid.Empty) {
                                c.Method = methodID;
                                RegisterCommand(c.CommandString, c);
                                mappedCommands++;

                            } else
                                Core.Log("Command '" + commandKey + "' not valid. Method name is wrong.", LogType.ERROR);
                        }
                    }
                }
                catch (FormatException) {
                    Core.Log("Failed to register command '{0}': Invalid mapping format.", LogType.EXTENSIONS);
                }
            }

            CommandMap sys = new CommandMap();
            sys.CommandString = "system";
            sys.Method = Guid.Empty;
            sys.Extension = Guid.Empty;

            sys.AccessLevel = (byte) Core.SystemConfig.Configs["CommandAccess"].GetInt("sys", 250);
            RegisterCommand("sys", sys);

            sys.AccessLevel = (byte) Core.SystemConfig.Configs["CommandAccess"].GetInt("system", 250);
            RegisterCommand("system", sys);

            RegisterCommand("test", new CommandMap() { AccessLevel = 250 });
            RegisterCommand("commands", new CommandMap() { AccessLevel = 1 });
            RegisterCommand("help", new CommandMap() { AccessLevel = 1 });
            return mappedCommands;
        }

        public CommandMap[] GetAvailableCommandsForUser(User u) {
            List<CommandMap> available = new List<CommandMap>();

            foreach (KeyValuePair<String, CommandMap> cm in CommandManager.Instance.CommandMapping) {
                if (ExtensionSystem.Instance.Extensions.ContainsKey(cm.Value.Extension)) {
                    if (ExtensionSystem.Instance.Extensions[cm.Value.Extension].Ready)
                        if(cm.Value.AccessLevel <= u.NetworkAuthLevel)
                            available.Add(cm.Value);
                } else {
                    // Command is hard-coded into here
                    if (cm.Value.AccessLevel <= u.NetworkAuthLevel)
                        available.Add(cm.Value);
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
                case "test":
                    Core.Log("Got test args: " + message.message);
                    break;

                case "commands":
                    SystemCommands.HandleCommandsCommand(conn, message);
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
                            Core.Log("Recieved help command for command '" + subCommand + "'. Telling extension " + ext.Name + " to handle it. [methodID: " + mappedCommand.Method + "]", LogType.EXTENSIONS);
                            ext.HandleHelpCommandRecieved(conn, mappedCommand.Method, message);
                        } else {
                            response.type = Suibhne.Networks.Base.Reference.MessageType.PublicAction;
                            response.message = "does not have information on that command.";
                            conn.SendMessage(response);
                        }
                    }
                    break;

                default:
                    ExtensionMap extension = ExtensionSystem.Instance.Extensions[CommandMapping[command].Extension];

                    Core.Log("Recieved command '" + command + "'. Telling extension " + extension.Name + " to handle it. [methodID: " + cmd.Method + "]", LogType.EXTENSIONS);
                    if (!extension.Ready) {
                        response.message = "I have {" + command + "} registered as a command, but it looks like the extension isn't ready yet. Try again later.";
                        conn.SendMessage(response);
                    } else {
                        extension.HandleCommandRecieved(conn, cmd.Method, message);
                    }

                    break;
            }
        }

        private void HandleSystemCommand(NetworkBot conn, Message msg) {
            string[] messageParts = msg.message.Split(' ');
            String subCommand = "";
            Message response = new Message(msg.locationID, conn.Me, "System Command Response");

            if (messageParts.Length > 1)
                subCommand = messageParts[1];

            if (messageParts.Length > 1 && subCommand != "") {
                switch (subCommand) {
                    case "exts":
                    case "extensions":
                        #region Extensions System Handling
                        SystemCommands.HandleExtensionsCommand(conn, msg);
                        #endregion
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
                                        Core.SystemConfig.Reload();
                                        int numRemapped = MapCommands();
                                        response.message = "Successfully remapped " + numRemapped + " commands to " + ExtensionSystem.Instance.Extensions.Count + " extensions.";
                                        conn.SendMessage(response);

                                        // TODO: MapAccessLevels();
                                        Core.ConfigLastUpdate = DateTime.Now;
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
                                response.message = "is not sure what to do with that. Try config or extensions as a paremeter.";
                                response.type = Networks.Base.Reference.MessageType.PublicAction;
                                conn.SendMessage(response);
                                response.type = Networks.Base.Reference.MessageType.PublicMessage;
                                break;
                        }
                        break;

                    case "version":
                        SystemCommands.HandleVersionCommand(conn, msg);
                        break;

                    case "netinfo":
                        SystemCommands.HandleNetworkInfoCommand(conn, msg);
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
                        response.message = "Current identifier for location: " + msg.locationID + ". Network identifier: " + conn.Identifier;
                        conn.SendMessage(response);
                        break;

                    default:
                        response.type = Suibhne.Networks.Base.Reference.MessageType.PublicAction;
                        response.message = "does not know what you are asking for. "; // + "[Invalid subcommand]", Formatter.Colors.Orange);
                        conn.SendMessage(response);
                        response.type = Ostenvighx.Suibhne.Networks.Base.Reference.MessageType.PublicMessage;
                        break;
                }
            } else {
                response.message = "Available system commands: {exts/extensions, version, netinfo, uptime}";
                conn.SendMessage(response);
            }
        }

    }
}
