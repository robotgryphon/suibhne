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
using System.Net;
using System.Text;
using Ostenvighx.Suibhne;
using Ostenvighx.Suibhne.Networks.Base;

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
        protected Dictionary<Guid, NetworkBot> bots;

        protected Dictionary<Guid, ExtensionMap> Extensions;

        protected Dictionary<String, CommandMap> CommandMapping;

        protected DateTime StartTime;

        protected ExtensionServer Server;
        
        private String ConfigFilename;
        private DateTime ConfigLastUpdate;

        public ExtensionSystem(String extensionConfig) {
            this.Registry = new Dictionary<Guid, object>();
            this.bots = new Dictionary<Guid, NetworkBot>();
            this.Identifier = Guid.NewGuid();
            this.ConfigFilename = extensionConfig;
            if (File.Exists(this.ConfigFilename)) {
                // Get some basic info about config file
                this.ConfigLastUpdate = File.GetLastWriteTime(ConfigFilename);
            }

            this.CommandMapping = new Dictionary<String, CommandMap>();
            this.Extensions = new Dictionary<Guid, ExtensionMap>();

            this.StartTime = DateTime.Now;

            InitializeExtensions();

            Server = new ExtensionServer();
            Server.OnDataRecieved += HandleIncomingData;
            Server.Start();
        }

        #region Registry
        public void AddBot(NetworkBot bot) {
            if (!this.bots.ContainsKey(bot.Identifier))
                bots.Add(bot.Identifier, bot);
        }

        

        protected void InitializeExtensions() {
            if (!File.Exists(this.ConfigFilename))
                throw new FileNotFoundException("Config file not valid.");


            IniConfigSource MainExtensionConfiguration = new IniConfigSource(this.ConfigFilename);

            Core.Log("Extension file last updated: " + File.GetLastWriteTime(this.ConfigFilename), LogType.EXTENSIONS);

            // Get ExtensionDirectories available via directory name
            String ExtensionsRootDirectory = MainExtensionConfiguration.Configs["Extensions"].GetString("extensionDir", Environment.CurrentDirectory + "/Extensions/");

            ExtensionMap[] exts = ExtensionLoader.LoadExtensions(ExtensionsRootDirectory);
            foreach (ExtensionMap extension in exts) {
                Extensions.Add(extension.Identifier, extension);
            }

            Core.Log("All extensions loaded into system.", LogType.EXTENSIONS);

            MapCommands();
        }

        private int MapCommands() {
            int mappedCommands = 0;
            if (!File.Exists(this.ConfigFilename))
                return 0;

            IniConfigSource MainExtensionConfiguration = new IniConfigSource(this.ConfigFilename);

            CommandMapping.Clear();
            String[] commands = MainExtensionConfiguration.Configs["Routing"].GetKeys();
            foreach (String commandKey in commands) {
                String commandMap = MainExtensionConfiguration.Configs["Routing"].GetString(commandKey);
                try {
                    CommandMap c = new CommandMap();
                    c.CommandString = commandKey.ToLower();
                    String ExtensionsRootDirectory = MainExtensionConfiguration.Configs["Extensions"].GetString("extensionDir", Environment.CurrentDirectory + "/Extensions/");
                    int nameEnd = commandMap.IndexOf(":");
                    if (nameEnd == -1) nameEnd = 0;

                    c.AccessLevel = (byte) MainExtensionConfiguration.Configs["Access"].GetInt(c.CommandString, 1);

                    String extensionDirectory = ExtensionsRootDirectory + commandMap.Substring(0, nameEnd);
                    if (Directory.Exists(extensionDirectory)) {
                        ExtensionMap em = ExtensionLoader.LoadExtension(extensionDirectory);
                        if (em.Identifier != Guid.Empty) {
                            c.Extension = em.Identifier;
                            Guid methodID = ExtensionLoader.GetMethodIdentifier(extensionDirectory, commandMap.Substring(nameEnd + 1).Trim());
                            if (methodID != Guid.Empty) {
                                c.Method = methodID;
                                CommandMapping.Add(c.CommandString, c);
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

            sys.AccessLevel = (byte)MainExtensionConfiguration.Configs["Access"].GetInt("sys", 250);
            CommandMapping.Add("sys", sys);

            sys.AccessLevel = (byte)MainExtensionConfiguration.Configs["Access"].GetInt("system", 250);
            CommandMapping.Add("system", sys);

            CommandMapping.Add("oplevel", new CommandMap() { AccessLevel = 1 });
            return mappedCommands + 2;
        }

        public void HandleCommand(NetworkBot conn, Message message) {
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

            if (!CommandMapping.ContainsKey(command))
                return;

            CommandMap cmd = CommandMapping[command];
            if (!(cmd.AccessLevel <= message.sender.NetworkAuthLevel)) {
                response.message = "You do not have permission to run this command.";
                conn.SendMessage(response);
                return;
            }

            // TODO: Create system commands extension and remove this from here. Clean this method up.
            switch (command) {
                case "oplevel":
                    response.message = "You have an access level of " + message.sender.NetworkAuthLevel + ", " + message.sender.DisplayName + ". Your LOCAL access level is " + message.sender.LocalAuthLevel + ".";
                    conn.SendMessage(response);
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
                                                String[] exts = GetExtensions();

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
                                                break;

                                            case "reload":
                                                switch (messageParts[3]) {
                                                    case "commands":
                                                        if (File.Exists(this.ConfigFilename)) {
                                                            DateTime lastUpdate = File.GetLastWriteTime(ConfigFilename);
                                                            if (lastUpdate > ConfigLastUpdate) {
                                                                int numRemapped = MapCommands();
                                                                response.message = "Successfully remapped " + numRemapped + " conmmands to " + Extensions.Count  + " extensions.";
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
                                            ((AssemblyTitleAttribute) networkTypeAssembly.GetCustomAttribute(typeof(AssemblyTitleAttribute))).Title + 
                                            " written by " + 
                                            ((AssemblyCompanyAttribute) networkTypeAssembly.GetCustomAttribute(typeof(AssemblyCompanyAttribute))).Company + 
                                            " (v" + networkTypeAssembly.GetName().Version + ")";

                                        conn.SendMessage(response);
                                    }
                                        
                                }

                                catch(Exception e){
                                    response.message = "There was an error processing the command. (" + e.GetType().Name + ")";
                                    conn.SendMessage(response);
                                }
                                break;

                            case "uptime":
                                TimeSpan diff = DateTime.Now - StartTime;
                                response.type = Ostenvighx.Suibhne.Networks.Base.Reference.MessageType.PublicAction;
                                response.message = "has been up for " +
                                    (diff.Days > 0 ? diff.Days + " days" : "") +
                                    (diff.Hours > 0 ? diff.Hours + " hours, " : "") +
                                    (diff.Minutes > 0 ? diff.Minutes + " minutes, " : "") +
                                    (diff.Seconds > 0 ? diff.Seconds + " seconds" : "") + ". [Up since " + StartTime.ToString() + "]";

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
                            ExtensionMap ext = Extensions[mappedCommand.Extension];
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
                    if (CommandMapping.ContainsKey(command)) {
                        CommandMap mappedCommand = CommandMapping[command];
                        ExtensionMap ext = Extensions[CommandMapping[command].Extension];
                        Core.Log("Recieved command '" + command + "'. Telling extension " + ext.Name + " to handle it. [methodID: " + mappedCommand.Method + "]", LogType.EXTENSIONS);
                        ext.HandleCommandRecieved(conn, mappedCommand.Method, message);
                    } else {
                        response.type = Suibhne.Networks.Base.Reference.MessageType.PublicAction;
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
                            out msg.sender.DisplayName,
                            out msg.message);

                        msg.type = (Ostenvighx.Suibhne.Networks.Base.Reference.MessageType)type;
                        msg.locationID = destination;

                        try {
                            NetworkBot bot = bots[Core.NetworkLocationMap[destination]];
                            bot.SendMessage(msg);
                        }

                        catch (KeyNotFoundException) {
                            // Network invalid or changed between requests
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
        /// <param name="id">IrcNetwork to check</param>
        /// <returns></returns>
        [InfoNode("listserv")]
        public ExtensionMap[] GetExtensions(Guid serverID) {
            return new ExtensionMap[0];
        }

    }
}

