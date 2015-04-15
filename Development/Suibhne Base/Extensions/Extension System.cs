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
using Raindrop.Api.Irc;
using System.Net;
using System.Text;
using Raindrop.Suibhne.Extensions;

namespace Raindrop.Suibhne {

    /// <summary>
    /// The extension registry connects an IrcBot and a set of extension suites together.
    /// It goes through the ExtensionDirectories directory defined in the bot configuration and
    /// searches through directories for the extension INI files.
    /// </summary>
    public class ExtensionSystem {

        public Guid Identifier;

        protected Dictionary<Guid, IrcBot> bots;

        protected Dictionary<Guid, ExtensionMap> Extensions;

        protected Dictionary<String, CommandMap> CommandMapping;

        public Socket Connection;

        protected byte[] Buffer;

        protected DateTime StartTime;

        public ExtensionSystem(String extensionConfig) {
            this.bots = new Dictionary<Guid, IrcBot>();
            this.Identifier = Guid.NewGuid();

            this.CommandMapping = new Dictionary<String, CommandMap>();
            this.Extensions = new Dictionary<Guid, ExtensionMap>();
            this.Connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            this.Buffer = new byte[1024];

            this.StartTime = DateTime.Now;

            InitializeExtensions(extensionConfig);

            StartServer();
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
                    c.Extension = new Guid(commandMap.Substring(0, commandMap.IndexOf(" ") + 1));
                    if (Extensions.ContainsKey(c.Extension)) {
                        ExtensionMap ext = Extensions[c.Extension];
                        String commandMethod = commandMap.Substring(commandMap.IndexOf(" ") + 1);

                        Core.Log("Mapping '" + c.CommandString + "' to extension " + ext.Name + ".", LogType.EXTENSIONS);
                        this.CommandMapping.Add(c.CommandString, c);
                    } else {
                        Core.Log("Command identifier not valid for command: " + commandKey, LogType.ERROR);
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

            Message response = new Message(message.location, conn.Me, "Response");
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

                                                    ExtensionMap[] exts = GetServerExtensions(conn.Identifier);

                                                    if (exts.Length > 0) {
                                                        byte[] originBytes = Encoding.UTF8.GetBytes(message.sender.nickname + " " + message.location);
                                                        byte[] request = new byte[17 + originBytes.Length];
                                                        request[0] = (byte)Extension.ResponseCodes.Details;
                                                        Array.Copy(conn.Identifier.ToByteArray(), 0, request, 1, 16);
                                                        Array.Copy(originBytes, 0, request, 18, 16);

                                                        foreach (ExtensionMap ext in exts) {
                                                            ext.Send(request);
                                                        }
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
                                    response.type = Raindrop.Api.Irc.Reference.MessageType.ChannelAction;
                                    response.message = "is currently running version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                                    conn.SendMessage(response);
                                    response.type = Raindrop.Api.Irc.Reference.MessageType.ChannelMessage;
                                    break;


                                case "raw":
                                    string rawCommand = message.message.Split(new char[] { ' ' }, 3)[2];
                                    conn.SendRaw(rawCommand);

                                    break;

                                case "uptime":
                                    TimeSpan diff = DateTime.Now - StartTime;
                                    response.type = Raindrop.Api.Irc.Reference.MessageType.ChannelAction;
                                    response.message = "has been up for " +
                                        (diff.Days > 0 ? Formatter.GetColoredText(diff.Days + " days", Formatter.Colors.Pink) + ", " : "") +
                                        (diff.Hours > 0 ? Formatter.GetColoredText(diff.Hours + " hours", Formatter.Colors.Orange) + ", " : "") +
                                        (diff.Minutes > 0 ? Formatter.GetColoredText(diff.Minutes + " minutes", Formatter.Colors.Green) + ", " : "") +
                                        (diff.Seconds > 0 ? Formatter.GetColoredText(diff.Seconds + " seconds", Formatter.Colors.Blue) : "") + ". [Up since " + StartTime.ToString() + "]";

                                    conn.SendMessage(response);
                                    response.type = Raindrop.Api.Irc.Reference.MessageType.ChannelMessage;
                                    break;

                                default:
                                    response.type = Api.Irc.Reference.MessageType.ChannelAction;
                                    response.message = "does not know what you are asking for. " + Formatter.GetColoredText("[Invalid subcommand]", Formatter.Colors.Orange);
                                    conn.SendMessage(response);
                                    response.type = Raindrop.Api.Irc.Reference.MessageType.ChannelMessage;
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

                default:
                    if (CommandMapping.ContainsKey(command)) {
                        CommandMap mappedCommand = CommandMapping[command];
                        ExtensionMap ext = Extensions[CommandMapping[command].Extension];
                        Core.Log("Recieved command '" + command + "'. Telling extension " + ext.Name + " to handle it.", LogType.EXTENSIONS);
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


        #region Server
        internal void StartServer() {
            Core.Log("Setting up server..", LogType.EXTENSIONS);

            Connection.Bind(new IPEndPoint(IPAddress.Any, 6700));
            Connection.Listen(5);

            Connection.BeginAccept(new AsyncCallback(AcceptConnection), null);
            Core.Log("Server setup complete. Extensions system ready.", LogType.EXTENSIONS);
            Console.WriteLine();
        }

        #region Socket Handling Callbacks
        protected void AcceptConnection(IAsyncResult result) {
            try {
                Socket s = Connection.EndAccept(result);
                Core.Log("Connected extension.", LogType.EXTENSIONS);
                s.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, RecieveDataCallback, s);
                Connection.BeginAccept(AcceptConnection, null);
            }

            catch (ObjectDisposedException) {
                // Socket exposed, this is on bot shutdown usually
            }

            catch (Exception) { }
        }

        protected void RecieveDataCallback(IAsyncResult result) {
            Socket recievedOn = (Socket)result.AsyncState;
            try {
                int recievedAmount = recievedOn.EndReceive(result);

                if (recievedAmount > 0) {
                    byte[] btemp = new byte[recievedAmount];
                    Array.Copy(Buffer, btemp, recievedAmount);

                    HandleIncomingData(recievedOn, btemp);

                    recievedOn.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, RecieveDataCallback, recievedOn);
                } else {
                    recievedOn.Shutdown(SocketShutdown.Both);
                    RemoveBySocket(recievedOn, "Extension shut down.");
                }
            }

            catch (SocketException) {
                RemoveBySocket(recievedOn, "Extension crashed.");
            }
        }
        #endregion

        protected void HandleIncomingData(Socket sock, byte[] data) {
            Extension.ResponseCodes code = (Extension.ResponseCodes)data[0];
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

                    case Extension.ResponseCodes.Activation:
                        Console.WriteLine(origin);

                        if (suite.Socket == null) {
                            suite.Socket = sock;
                        }

                        suite.Ready = true;

                        Extensions[origin] = suite;
                        break;

                    case Extension.ResponseCodes.Details:
                        Console.WriteLine("Recieving extension details");
                        String suiteName = Encoding.UTF8.GetString(extraData);
                        suite.Name = suiteName;
                        break;

                    case Extension.ResponseCodes.Remove:
                        sock.Shutdown(SocketShutdown.Both);
                        sock.Close();
                        return;

                    case Extension.ResponseCodes.Message:
                        Message msg = new Message("", new User(), "");
                        Guid destination;
                        byte type = 1;

                        Extension.ParseMessage(
                            data,
                            out origin,
                            out destination,
                            out type,
                            out msg.location,
                            out msg.sender.nickname,
                            out msg.message);

                        msg.type = (Raindrop.Api.Irc.Reference.MessageType)type;

                        try {
                            IrcBot bot = bots[destination];
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

        protected void RemoveBySocket(Socket s, string reason = "") {
            foreach (KeyValuePair<Guid, ExtensionMap> extension in Extensions) {
                if (extension.Value.Socket == s) {
                    Extensions.Remove(extension.Key);
                    Core.Log("Extension '" + extension.Value.Name + "' removed: " + reason, LogType.EXTENSIONS);
                    return;
                }
            }
        }

        internal void Shutdown() {
            foreach (KeyValuePair<Guid, ExtensionMap> ext in Extensions) {
                ext.Value.Send(new byte[] { (byte)Extension.ResponseCodes.Remove });
                ext.Value.Socket.Shutdown(SocketShutdown.Both);
            }

            Extensions.Clear();
            Connection.Close();
        }

        // TODO: Start tracking which ExtensionDirectories are enabled on which server

        internal ExtensionMap[] GetServerExtensions(Guid id) {
            return new ExtensionMap[0];

        }
        #endregion

    }
}

