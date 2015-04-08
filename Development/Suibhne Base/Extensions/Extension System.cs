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

        protected Dictionary<Guid, ExtensionReference> Extensions;

        protected Dictionary<String, CommandMap> CommandMapping;

        public Socket Connection;

        public static byte[] spaceBytes = Encoding.UTF8.GetBytes(" ");

        protected byte[] Buffer;

        protected DateTime StartTime;

        public ExtensionSystem(String extensionConfig) {
            this.bots = new Dictionary<Guid, IrcBot>();
            this.Identifier = Guid.NewGuid();

            this.CommandMapping = new Dictionary<String, CommandMap>();
            this.Extensions = new Dictionary<Guid, ExtensionReference>();
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

                Console.WriteLine("[Extensions System] Routing last updated: " + MainExtensionConfiguration.Configs["Extensions"].GetString("updated"));
                Console.WriteLine();

                // Get ExtensionDirectories available via directory name
                String ExtensionsRootDirectory = MainExtensionConfiguration.Configs["Extensions"].GetString("extensionDir", Environment.CurrentDirectory + "/Extensions/");

                if (Directory.Exists(ExtensionsRootDirectory)) {
                    String[] ExtensionDirectories = Directory.GetDirectories(ExtensionsRootDirectory);
                    foreach (String ExtensionDirectory in ExtensionDirectories) {
                        String ExtensionIdentifier = ExtensionDirectory.Substring(ExtensionDirectory.LastIndexOf("/") + 1);
                        String extensionConfigurationFile = ExtensionDirectory + "/extension.ini";

                        // Console.WriteLine("[Extensions System] Got extension config file: " + extensionConfigurationFile);

                        // Try to find extension.ini file in there
                        if (File.Exists(extensionConfigurationFile)) {
                            // Found extension config file, begin parsing
                            IniConfigSource extensionConfig = new IniConfigSource(extensionConfigurationFile);
                            extensionConfig.CaseSensitive = false;

                            ExtensionReference extensionRef = new ExtensionReference();

                            extensionRef.Identifier = new Guid(extensionConfig.Configs["Extension"].GetString("identifier", Guid.NewGuid().ToString()));
                            extensionRef.Methods = new List<Guid>();
                            extensionRef.Ready = false;

                            Console.WriteLine("[Extensions System - {0}] Registered identifier string: {1}", ExtensionIdentifier, extensionRef.Identifier);

                            Console.WriteLine("[Extensions System - {0}] Updated on {1}", ExtensionIdentifier,
                                extensionConfig.Configs["Extension"].GetString("updated", DateTime.Now.ToString()));

                            Console.WriteLine("[Extensions System - {0}] Registered methods: {1}", ExtensionIdentifier,
                                String.Join(", ", extensionConfig.Configs["Routing"].GetKeys()));

                            foreach (String s in extensionConfig.Configs["Routing"].GetKeys())
                                extensionRef.Methods.Add(new Guid(extensionConfig.Configs["Routing"].GetString(s, Guid.NewGuid().ToString())));

                            Extensions.Add(extensionRef.Identifier, extensionRef);

                            Console.WriteLine();
                        }
                    }
                } else {
                    return;
                }

                String[] commands = MainExtensionConfiguration.Configs["Routing"].GetKeys();
                foreach (String commandKey in commands) {
                    String commandMap = MainExtensionConfiguration.Configs["Routing"].GetString(commandKey);
                    try {

                        CommandMap c = new CommandMap();
                        c.CommandString = commandKey.ToLower();
                        c.Extension = new Guid(commandMap.Substring(0, commandMap.IndexOf(" ") + 1));
                        if (Extensions.ContainsKey(c.Extension)) {
                            ExtensionReference ext = Extensions[c.Extension];
                            String commandMethod = commandMap.Substring(commandMap.IndexOf(" ") + 1);

                            // Time to map from the extension routing table to the actual thing
                            IniConfigSource extensionConfig = new IniConfigSource(ExtensionsRootDirectory + "/" + c.Extension + "/extension.ini");
                            extensionConfig.CaseSensitive = false;

                            if (extensionConfig.Configs["Routing"].Contains(commandMethod))
                                c.Method = new Guid(extensionConfig.Configs["Routing"].GetString(commandMethod));

                            Console.WriteLine("[Extensions System] Registering command '{0}' to extension '{1}' (method: {2})", commandKey, ext.Identifier, c.Method);

                            CommandMapping.Add(commandKey, c);
                        } else {

                        }

                    }
                    catch (FormatException) {
                        Console.WriteLine("[Extensions System] Failed to register command '{0}': Invalid mapping format.");
                    }
                }
            } else {
                throw new FileNotFoundException("Config file not valid.");
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

                                                    ExtensionReference[] exts = GetServerExtensions(conn.Identifier);

                                                    if (exts.Length > 0) {
                                                        byte[] originBytes = Encoding.UTF8.GetBytes(message.sender.nickname + " " + message.location);
                                                        byte[] request = new byte[17 + originBytes.Length];
                                                        request[0] = (byte)Extension.ResponseCodes.Details;
                                                        Array.Copy(conn.Identifier.ToByteArray(), 0, request, 1, 16);
                                                        Array.Copy(originBytes, 0, request, 18, 16);

                                                        foreach (ExtensionReference ext in exts) {
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

                        Extensions[CommandMapping[command].Extension].HandleCommandRecieved(conn, mappedCommand.Method, message);
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
            Console.WriteLine("[Extensions System] Setting up server..");

            Connection.Bind(new IPEndPoint(IPAddress.Any, 6700));
            Connection.Listen(5);

            Connection.BeginAccept(new AsyncCallback(AcceptConnection), null);
            Console.WriteLine("[Extensions System] Server setup complete. Extensions system ready.");
        }

        #region Socket Handling Callbacks
        protected void AcceptConnection(IAsyncResult result) {
            try {
                Socket s = Connection.EndAccept(result);

                Console.WriteLine("[Extensions System] Connected extension.");

                s.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, RecieveDataCallback, s);

                Connection.BeginAccept(AcceptConnection, null);
            }

            catch (ObjectDisposedException) {
                // Socket exposed, this is on bot shutdown usually
            }

            catch (Exception e) {
                Console.WriteLine(e);
            }
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

                ExtensionReference suite = Extensions[origin];


                Console.WriteLine("Handling response code {0} from suite {1}.", code, suite.Name);

                #region Handle Code Response
                switch (code) {

                    case Extension.ResponseCodes.Activation:
                        Console.WriteLine(origin);

                        if (suite.Socket == null) {
                            suite.Socket = sock;
                        }

                        suite.Ready = true;

                        Extensions[suite.Identifier] = suite;
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
                Console.WriteLine(e);
            }
        }

        protected void RemoveBySocket(Socket s, string reason = "") {
            foreach (KeyValuePair<Guid, ExtensionReference> extension in Extensions) {
                if (extension.Value.Socket == s) {
                    Extensions.Remove(extension.Key);
                    Console.WriteLine("[Extensions System] Extension '" + extension.Value.Name + "' removed: " + reason);
                    return;
                }
            }
        }

        internal void Shutdown() {
            foreach (KeyValuePair<Guid, ExtensionReference> ext in Extensions) {
                ext.Value.Send(new byte[] { (byte)Extension.ResponseCodes.Remove });
                ext.Value.Socket.Shutdown(SocketShutdown.Both);
            }

            Extensions.Clear();
            Connection.Close();
        }

        // TODO: Start tracking which ExtensionDirectories are enabled on which server

        internal ExtensionReference[] GetServerExtensions(Guid id) {
            return new ExtensionReference[0];

        }
        #endregion

    }
}

