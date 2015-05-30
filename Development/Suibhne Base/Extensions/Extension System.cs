using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

using Nini.Config;

using System.Diagnostics;
using System.Threading;
using System.Net.Sockets;
using Ostenvighx.Api.Irc;
using System.Net;
using System.Text;
using Ostenvighx.Suibhne;

namespace Ostenvighx.Suibhne.Extensions {

    [InfoNode("extensions")]
    /// <summary>
    /// The extension registry connects an IrcBot and a set of extension suites together.
    /// It goes through the ExtensionDirectories directory defined in the bot configuration and
    /// searches through directories for the extension INI files.
    /// </summary>
    public class ExtensionSystem {

        public Guid Identifier;
        public Dictionary<Guid, Object> Registry;
        protected Dictionary<Guid, IrcBot> bots;

        protected Dictionary<Guid, ExtensionMap> Extensions;

        protected Dictionary<String, CommandMap> CommandMapping;

        protected DateTime StartTime;

        protected ExtensionServer Server;

        public ExtensionSystem(String extensionConfig) {
            this.Registry = new Dictionary<Guid, object>();
            this.bots = new Dictionary<Guid, IrcBot>();
            this.Identifier = Guid.NewGuid();

            this.CommandMapping = new Dictionary<String, CommandMap>();
            this.Extensions = new Dictionary<Guid, ExtensionMap>();

            this.StartTime = DateTime.Now;

            InitializeExtensions(extensionConfig);

            Server = new ExtensionServer();
            Server.OnDataRecieved += HandleIncomingData;
            Server.Start();
        }

        #region Registry
        public void AddBot(IrcBot bot) {
            if (!this.bots.ContainsKey(bot.Identifier))
                bots.Add(bot.Identifier, bot);
        }

        

        protected void InitializeExtensions(String config) {
            if (File.Exists(config)) {
                IniConfigSource MainExtensionConfiguration = new IniConfigSource(config);

                Core.Log("Routing last updated: " + MainExtensionConfiguration.Configs["Extensions"].GetString("updated"), LogType.EXTENSIONS);

                // Get ExtensionDirectories available via directory name
                String ExtensionsRootDirectory = MainExtensionConfiguration.Configs["Extensions"].GetString("extensionDir", Environment.CurrentDirectory + "/Extensions/");

                ExtensionMap[] exts = Extension_Loader.LoadExtensions(ExtensionsRootDirectory);
                foreach (ExtensionMap extension in exts) {
                    Extensions.Add(extension.Identifier, extension);
                }

                Core.Log("All extensions loaded into system.", LogType.EXTENSIONS);

                MapCommands(MainExtensionConfiguration);
            } else {
                throw new FileNotFoundException("Config file not valid.");
            }
        }

        private void MapCommands(IniConfigSource config) {
            CommandMapping.Clear();
            String[] commands = config.Configs["Routing"].GetKeys();
            foreach (String commandKey in commands) {
                String commandMap = config.Configs["Routing"].GetString(commandKey);
                try {
                    CommandMap c = new CommandMap();
                    c.CommandString = commandKey.ToLower();
                    String ExtensionsRootDirectory = config.Configs["Extensions"].GetString("extensionDir", Environment.CurrentDirectory + "/Extensions/");
                    int nameEnd = commandMap.IndexOf(":");
                    if (nameEnd == -1) nameEnd = 0;

                    String extensionDirectory = ExtensionsRootDirectory + commandMap.Substring(0, nameEnd);
                    if (Directory.Exists(extensionDirectory)) {
                        ExtensionMap em = Extension_Loader.LoadExtension(extensionDirectory);
                        if (em.Identifier != Guid.Empty) {
                            c.Extension = em.Identifier;
                            Guid methodID = Extension_Loader.GetMethodIdentifier(extensionDirectory, commandMap.Substring(nameEnd + 1).Trim());
                            if (methodID != Guid.Empty) {
                                c.Method = methodID;
                                CommandMapping.Add(c.CommandString, c);
                            } else
                                Core.Log("Command '" + commandKey + "' not valid. Method name is wrong.", LogType.ERROR);
                        }
                    }
                }
                catch (FormatException) {
                    Core.Log("Failed to register command '{0}': Invalid mapping format.", LogType.EXTENSIONS);
                }
            }
        }

        public void HandleCommand(IrcBot conn, Message message) {
            String[] messageParts = message.message.Split(new char[] { ' ' });
            String command = messageParts[0].ToLower().TrimStart(new char[] { '!' }).TrimEnd();
            String subCommand = "";
            if (messageParts.Length > 1)
                subCommand = messageParts[1].ToLower();

            Message response = new Message(message.locationID, conn.Me, "Response");
            response.type = Api.Irc.Reference.MessageType.ChannelMessage;

            // TODO: Create system commands extension and remove this from here. Clean this method up.
            switch (command) {
                case "sys":
                    #region System Commands
                    if (messageParts.Length > 1 && subCommand != "") {
                        if (conn.IsBotOperator(message.sender.nickname)) {
                            switch (subCommand) {
                                case "exts":
                                    #region Extensions System Handling
                                    switch (messageParts.Length) {
                                        case 2:
                                            response.message = "Invalid Parameters. Format: !sys exts [command]";
                                            conn.SendMessage(response);
                                            break;

                                        case 3:
                                            subCommand = messageParts[2];
                                            switch (subCommand.ToLower()) {
                                                case "list":
                                                    response.message = "Gathering data for global extension list. May take a minute.";
                                                    conn.SendMessage(response);

                                                    String[] exts = GetExtensions();

                                                    if (exts.Length > 0) {
                                                        response.message = String.Join(", ", exts);
                                                        conn.SendMessage(response);
                                                    } else {
                                                        response.message = "No extensions loaded on server.";
                                                        conn.SendMessage(response);
                                                    }
                                                    break;

                                                default:
                                                    response.message = "Unknown command.";
                                                    conn.SendMessage(response);
                                                    break;
                                            }

                                            break;

                                        case 4:
                                            // TODO: Manage extension system [enable, disable, remap commands, etc]
                                            break;
                                    }
                                    #endregion
                                    break;

                                case "version":
                                    response.type = Ostenvighx.Api.Irc.Reference.MessageType.ChannelAction;
                                    response.message = "is currently running version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                                    conn.SendMessage(response);
                                    response.type = Ostenvighx.Api.Irc.Reference.MessageType.ChannelMessage;
                                    break;


                                case "raw":
                                    string rawCommand = message.message.Split(new char[] { ' ' }, 3)[2];
                                    conn.SendRaw(rawCommand);

                                    break;

                                case "uptime":
                                    TimeSpan diff = DateTime.Now - StartTime;
                                    response.type = Ostenvighx.Api.Irc.Reference.MessageType.ChannelAction;
                                    response.message = "has been up for " +
                                        (diff.Days > 0 ? Formatter.GetColoredText(diff.Days + " days", Formatter.Colors.Pink) + ", " : "") +
                                        (diff.Hours > 0 ? Formatter.GetColoredText(diff.Hours + " hours", Formatter.Colors.Orange) + ", " : "") +
                                        (diff.Minutes > 0 ? Formatter.GetColoredText(diff.Minutes + " minutes", Formatter.Colors.Green) + ", " : "") +
                                        (diff.Seconds > 0 ? Formatter.GetColoredText(diff.Seconds + " seconds", Formatter.Colors.Blue) : "") + ". [Up since " + StartTime.ToString() + "]";

                                    conn.SendMessage(response);
                                    response.type = Ostenvighx.Api.Irc.Reference.MessageType.ChannelMessage;
                                    break;

                                default:
                                    response.type = Api.Irc.Reference.MessageType.ChannelAction;
                                    response.message = "does not know what you are asking for. " + Formatter.GetColoredText("[Invalid subcommand]", Formatter.Colors.Orange);
                                    conn.SendMessage(response);
                                    response.type = Ostenvighx.Api.Irc.Reference.MessageType.ChannelMessage;
                                    break;
                            }
                        } else {
                            response.message = Formatter.GetColoredText("Error: ", Formatter.Colors.Red) + "You must be a bot operator to run the system command.";
                            conn.SendMessage(response);
                        }
                    } else {
                        response.message = Formatter.GetColoredText("Error: ", Formatter.Colors.Red) + "System command takes at least a single parameter. Try raw, version, or exts.";
                        conn.SendMessage(response);
                    }
                    #endregion
                    break;

                case "help":
                    if (subCommand != "") {
                        // Map command to id
                        if (CommandMapping.ContainsKey(subCommand)) {
                            CommandMap mappedCommand = CommandMapping[subCommand];
                            ExtensionMap ext = Extensions[mappedCommand.Extension];
                            Core.Log("Recieved help command for command '" + subCommand + "'. Telling extension " + ext.Name + " to handle it. [methodID: " + mappedCommand.Method + "]", LogType.EXTENSIONS);
                            ext.HandleHelpCommandRecieved(conn, mappedCommand.Method, message);
                        } else {
                            response.type = Api.Irc.Reference.MessageType.ChannelAction;
                            response.message = "does not have information on that command.";
                            conn.SendMessage(response);
                        }
                    }
                    break;

                default:
                    if (CommandMapping.ContainsKey(command)) {
                        CommandMap mappedCommand = CommandMapping[command];
                        ExtensionMap ext = Extensions[CommandMapping[command].Extension];
                        Core.Log("Recieved command '" + command + "'. Telling extension " + ext.Name + " to handle it. [methodID: " + mappedCommand.Method + "]", LogType.EXTENSIONS);
                        ext.HandleCommandRecieved(conn, mappedCommand.Method, message);
                    } else {
                        response.type = Api.Irc.Reference.MessageType.ChannelAction;
                        response.message = "is not sure what to do with this information. [INVALID COMMAND]";
                        conn.SendMessage(response);
                    }
                    break;
            }
        }
        #endregion

        protected void HandleIncomingData(Socket sock, byte[] data) {
            Responses code = (Responses)data[0];
            byte[] guidBytes = new byte[16];
            Array.Copy(data, 1, guidBytes, 0, 16);

            Guid origin = new Guid(guidBytes);
            byte[] extraData = new byte[0];
            if (data.Length > 17) {
                extraData = new byte[data.Length - 17];
                Array.Copy(data, 17, extraData, 0, extraData.Length);
            }

            // Get the extension suite off the returned Identifier first
            try {

                ExtensionMap suite = Extensions[origin];


                #region Handle Code Response
                switch (code) {

                    case Responses.Activation:
                        Core.Log("Activating extension: " + Extensions[origin].Name, LogType.EXTENSIONS);

                        if (suite.Socket == null) {
                            suite.Socket = sock;
                        }

                        suite.Ready = true;

                        Extensions[origin] = suite;
                        break;

                    case Responses.Details:
                        Console.WriteLine("Recieving extension details");
                        String suiteName = Encoding.UTF8.GetString(extraData);
                        suite.Name = suiteName;
                        break;

                    case Responses.Remove:
                        sock.Shutdown(SocketShutdown.Both);
                        sock.Close();
                        return;

                    case Responses.Message:
                        Message msg = new Message(Guid.Empty, new User(), "");
                        Guid destination;
                        byte type = 1;

                        // TODO: Need to create global location registry
                        Extension.ParseMessage(
                            data,
                            out origin,
                            out destination,
                            out type,
                            out msg.sender.nickname,
                            out msg.message);

                        msg.type = (Ostenvighx.Api.Irc.Reference.MessageType)type;
                        msg.locationID = destination;

                        try {
                            IrcBot bot = bots[Core.NetworkLocationMap[destination]];
                            bot.SendMessage(msg);
                        }

                        catch (KeyNotFoundException) {
                            // Server invalid or changed between requests
                        }

                        break;

                    default:
                        // Unknown response

                        break;

                }
                #endregion
            }
            catch (Exception e) {
                Core.Log("Extension callback: " + e.Message, LogType.ERROR);
            }
        }

        /// <summary>
        /// Gets a list of all active extensions.
        /// </summary>
        /// <returns></returns>
        [InfoNode("list")]
        public String[] GetExtensions() {
            List<String> maps = new List<String>();
            foreach (ExtensionMap map in Extensions.Values) {
                maps.Add(map.Name + ": " + map.Identifier);
            }
            return maps.ToArray();
        }

        // TODO: Start tracking which Extensions are enabled on which server
        /// <summary>
        /// Gets a list of all active extensions on a particular server.
        /// </summary>
        /// <param name="id">Connection to check</param>
        /// <returns></returns>
        [InfoNode("listserv")]
        public ExtensionMap[] GetExtensions(Guid serverID) {
            return new ExtensionMap[0];
        }

    }
}

