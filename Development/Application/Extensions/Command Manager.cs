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

        public CommandManager() {
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
            if (!File.Exists(Core.SystemConfigFilename))
                return 0;

            IniConfigSource MainExtensionConfiguration = new IniConfigSource(Core.SystemConfigFilename);

            WipeCommandMap();

            String[] commands = MainExtensionConfiguration.Configs["Commands"].GetKeys();
            foreach (String commandKey in commands) {
                String commandMap = MainExtensionConfiguration.Configs["Commands"].GetString(commandKey);
                try {
                    CommandMap c = new CommandMap();
                    c.CommandString = commandKey.ToLower();
                    String ExtensionsRootDirectory = MainExtensionConfiguration.Configs["Directories"].GetString("ExtensionsBinDirectory", Environment.CurrentDirectory + "/Extensions/");
                    int nameEnd = commandMap.IndexOf(":");
                    if (nameEnd == -1) nameEnd = 0;

                    c.AccessLevel = (byte)MainExtensionConfiguration.Configs["CommandAccess"].GetInt(c.CommandString, 1);

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

            sys.AccessLevel = (byte)MainExtensionConfiguration.Configs["CommandAccess"].GetInt("sys", 250);
            RegisterCommand("sys", sys);

            sys.AccessLevel = (byte)MainExtensionConfiguration.Configs["CommandAccess"].GetInt("system", 250);
            RegisterCommand("system", sys);

            RegisterCommand("test", new CommandMap() { AccessLevel = 250 });
            RegisterCommand("commands", new CommandMap() { AccessLevel = 1 });
            RegisterCommand("help", new CommandMap() { AccessLevel = 1 });
            return mappedCommands + 4;
        }

        public void HandleCommand(ExtensionSystem extensionSystem, NetworkBot conn, Message message) {
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

            // TODO: Create system commands extension and remove this from here. Clean this method up.
            switch (command) {
                case "test":
                    string node = message.message.Split(new char[] { ' ' }, 3)[1];
                    Dictionary<String, MemberInfo> coreNodes = Core.VariableNodes;

                    if (Core.VariableNodes.ContainsKey(node)) {
                        MemberInfo nodeObject = Core.VariableNodes[node];
                        response.message = "Got object: " + nodeObject.Name;

                        switch (nodeObject.MemberType) {

                            case MemberTypes.Method:

                                break;

                            case MemberTypes.Field:
                                response.message = "Got field: ";
                                if (nodeObject.DeclaringType == typeof(ExtensionSystem)) {
                                    Core.Log("Field lookup initiated: " + nodeObject.Name);
                                    response.message += nodeObject.DeclaringType.GetField(nodeObject.Name).GetValue(ExtensionSystem.Instance).ToString();
                                }
                                break;

                            case MemberTypes.TypeInfo:

                                break;
                        }

                        conn.SendMessage(response);
                    }

                    break;

                case "commands":
                    response.type = Networks.Base.Reference.MessageType.PublicAction;
                    response.message = "has these commands available: ";
                    List<String> available = new List<string>();
                    foreach (KeyValuePair<String, CommandMap> cm in CommandMapping) {
                        if (extensionSystem.Extensions.ContainsKey(cm.Value.Extension)) {
                            if (extensionSystem.Extensions[cm.Value.Extension].Ready)
                                available.Add(cm.Key);
                        } else {
                            // Command is hard-coded into here
                            available.Add(cm.Key + ((cm.Value.AccessLevel > 1) ? (" (" + cm.Value.AccessLevel.ToString() + ")") : ""));
                        }
                    }

                    response.message += String.Join(", ", available.ToArray());

                    conn.SendMessage(response);
                    response.type = Networks.Base.Reference.MessageType.PublicMessage;
                    break;

                case "sys":
                case "system":
                    #region System Commands
                    if (messageParts.Length > 1 && subCommand != "") {
                        switch (subCommand) {
                            case "exts":
                            case "extensions":
                                #region Extensions System Handling
                                switch (messageParts.Length) {
                                    case 3:
                                        #region Tier 3
                                        subCommand = messageParts[2];
                                        switch (subCommand.ToLower()) {
                                            case "list":
                                                String[] exts = extensionSystem.GetActiveExtensions();

                                                if (exts.Length > 0) {
                                                    response.message = String.Join(", ", exts);
                                                    conn.SendMessage(response);
                                                } else {
                                                    response.message = "No extensions loaded.";
                                                    conn.SendMessage(response);
                                                }
                                                break;

                                            default:
                                                response.message = "Unknown command. Available commands: {list, enable [ext], disable [ext], reload [type]}";
                                                conn.SendMessage(response);
                                                break;
                                        }

                                        #endregion
                                        break;

                                    case 4:
                                        #region Tier 4
                                        subCommand = messageParts[2];
                                        switch (subCommand.ToLower()) {
                                            case "enable":
                                                // Used for enabling an extension that was disabled during loading
                                                break;

                                            case "disable":
                                                // Used to disable a currently active extension
                                                if (extensionSystem.Extensions.ContainsKey(new Guid(messageParts[3]))) {
                                                    ExtensionMap ext = extensionSystem.Extensions[new Guid(messageParts[3])];
                                                    // ext.Stop();
                                                    extensionSystem.ShutdownExtensionBySocket(ext.Socket);
                                                    response.message = "Disabled extension: " + ext.Name;
                                                    conn.SendMessage(response);
                                                } else {
                                                    response.message = "That extension does not exist in the list. Please check the identifier and try again.";
                                                    conn.SendMessage(response);
                                                }
                                                break;

                                            case "reload":
                                                switch (messageParts[3]) {
                                                    case "commands":
                                                        if (File.Exists(Core.SystemConfigFilename)) {
                                                            DateTime lastUpdate = File.GetLastWriteTime(Core.SystemConfigFilename);
                                                            if (lastUpdate > extensionSystem.ConfigLastUpdate) {
                                                                int numRemapped = MapCommands();
                                                                response.message = "Successfully remapped " + numRemapped + " conmmands to " + extensionSystem.Extensions.Count + " extensions.";
                                                                conn.SendMessage(response);
                                                            } else {
                                                                response.message = "Your extension config is up-to-date. No need to remap commands.";
                                                                conn.SendMessage(response);
                                                            }
                                                        }
                                                        break;

                                                    case "extensions":
                                                        // WIP
                                                        break;

                                                }
                                                break;


                                            default:
                                                response.message = "Unknown command. Available commands: {enable, disable, reload}";
                                                conn.SendMessage(response);
                                                break;

                                        }
                                        #endregion
                                        break;

                                    default:
                                        response.message = "Subcommand required. Available commands: {list, enable [ext], disable [ext], reload [type]}";
                                        conn.SendMessage(response);
                                        break;
                                }
                                #endregion
                                break;

                            case "version":
                                response.type = Ostenvighx.Suibhne.Networks.Base.Reference.MessageType.PublicAction;
                                response.message = "is currently running version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                                conn.SendMessage(response);
                                response.type = Ostenvighx.Suibhne.Networks.Base.Reference.MessageType.PublicMessage;
                                break;

                            case "conninfo":
                                // Get info about connection and system
                                if (messageParts.Length != 3) {
                                    response.message = "Invalid Parameters. Format: !sys conninfo [connectionType]";
                                    conn.SendMessage(response);
                                    break;
                                }

                                try {
                                    string connType = messageParts[2];
                                    response.message = "Connection type recieved: " + connType;
                                    IniConfigSource configFile = new IniConfigSource(Environment.CurrentDirectory + "/suibhne.ini");
                                    String configDir = configFile.Configs["Suibhne"].GetString("ConfigurationRoot", Environment.CurrentDirectory + "/Configuration/").Trim();

                                    if (File.Exists(configDir + "NetworkTypes/" + connType + ".dll")) {
                                        Assembly networkTypeAssembly = Assembly.LoadFrom(configDir + "NetworkTypes/" + connType + ".dll");
                                        response.message = "Assembly information: " +
                                            ((AssemblyTitleAttribute)networkTypeAssembly.GetCustomAttribute(typeof(AssemblyTitleAttribute))).Title +
                                            " written by " +
                                            ((AssemblyCompanyAttribute)networkTypeAssembly.GetCustomAttribute(typeof(AssemblyCompanyAttribute))).Company +
                                            " (v" + networkTypeAssembly.GetName().Version + ")";

                                        conn.SendMessage(response);
                                    }

                                }

                                catch (Exception e) {
                                    response.message = "There was an error processing the command. (" + e.GetType().Name + ")";
                                    conn.SendMessage(response);
                                }
                                break;

                            case "uptime":
                                TimeSpan diff = DateTime.Now - extensionSystem.StartTime;
                                response.type = Ostenvighx.Suibhne.Networks.Base.Reference.MessageType.PublicAction;
                                response.message = "has been up for " +
                                    (diff.Days > 0 ? diff.Days + " days" : "") +
                                    (diff.Hours > 0 ? diff.Hours + " hours, " : "") +
                                    (diff.Minutes > 0 ? diff.Minutes + " minutes, " : "") +
                                    (diff.Seconds > 0 ? diff.Seconds + " seconds" : "") + ". [Up since " + extensionSystem.StartTime.ToString() + "]";

                                conn.SendMessage(response);
                                response.type = Ostenvighx.Suibhne.Networks.Base.Reference.MessageType.PublicMessage;
                                break;

                            default:
                                response.type = Suibhne.Networks.Base.Reference.MessageType.PublicAction;
                                response.message = "does not know what you are asking for. "; // + "[Invalid subcommand]", Formatter.Colors.Orange);
                                conn.SendMessage(response);
                                response.type = Ostenvighx.Suibhne.Networks.Base.Reference.MessageType.PublicMessage;
                                break;
                        }
                    } else {
                        response.message = "Available system commands: {exts/extensions, version, conninfo, uptime}";
                        conn.SendMessage(response);
                    }
                    #endregion
                    break;

                case "help":
                    if (subCommand != "") {
                        // Map command to id
                        if (CommandMapping.ContainsKey(subCommand)) {
                            CommandMap mappedCommand = CommandMapping[subCommand];
                            ExtensionMap ext = extensionSystem.Extensions[mappedCommand.Extension];
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
                    ExtensionMap extension = extensionSystem.Extensions[CommandMapping[command].Extension];

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

    }
}
