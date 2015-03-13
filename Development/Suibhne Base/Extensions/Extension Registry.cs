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

namespace Raindrop.Suibhne.Extensions {

    /// <summary>
    /// The extension registry connects an IrcBot and a set of extension suites together.
    /// It goes through the extensions directory defined in the bot configuration and
    /// searches through directories for the extension INI files.
    /// </summary>
    public class ExtensionRegistry {

        public Guid Identifier;

        protected Dictionary<Guid, IrcBot> bots;

        protected Dictionary<Guid, ExtensionReference> Extensions;

        protected Dictionary<String, Guid> CommandMapping;

        public Socket Connection;

        public static byte[] spaceBytes = Encoding.UTF8.GetBytes(" ");

        protected byte[] Buffer;

        public ExtensionRegistry() {
            this.bots = new Dictionary<Guid, IrcBot>();
            this.Identifier = Guid.NewGuid();

            this.CommandMapping = new Dictionary<string, Guid>();
            this.Extensions = new Dictionary<Guid, ExtensionReference>();
            this.Connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.Extensions = new Dictionary<Guid, ExtensionReference>();

            this.Buffer = new byte[1024];

            // InitializeExtensions();
            StartServer();
        }

        /*
        public void InitializeExtensions()
        {
            try
            {
                String[] ExtensionDirectories = Directory.GetDirectories(bot.Configuration.ConfigDirectory + "Extensions/");

                foreach (String extDir in ExtensionDirectories)
                {
                    String extDirName = extDir.Substring(extDir.LastIndexOf("/") + 1);

                    // Attempt to find config file for extension. Start by getting all ini files.
                    String[] ExtensionFiles = Directory.GetFiles(extDir, "*.ini");
                    String foundFile = "";

                    foreach (String file in ExtensionFiles)
                    {
                        if (Path.GetFileName(file).ToLower().Equals("extension.ini"))
                        {
                            // Found file
                            foundFile = file;

                            // Now, poke the extension exe for life.
                            IniConfigSource extConfig = new IniConfigSource();
                            extConfig.Load(file);

                            String extExecutable = extConfig.Configs["Extension"].GetString("MainExecutable").Trim();
                            if(extExecutable != ""){
                                Process.Start(extDir + "/"+ extExecutable);
                            }
                        }
                    }

                    if (foundFile == "")
                    {
                        Console.WriteLine("[Extension System] Failed to load extension suite from directory '{0}'. Suite file not found.", extDirName);
                    }
                    
                }
            }

            catch (IOException ioe)
            {
                Console.WriteLine("Failed to open directory.");
                Console.WriteLine(ioe.Message);
            }
        } */

        #region Registry
        // TODO: Attach events
        public void AddBot(IrcBot bot) {
            if (!this.bots.ContainsKey(bot.Identifier))
                bots.Add(bot.Identifier, bot);
        }

        public void HandleCommand(IrcBot conn, IrcMessage message) {
            String[] messageParts = message.message.Split(new char[] { ' ' });
            String command = messageParts[0].ToLower().TrimStart(new char[] { '!' }).TrimEnd();
            String subCommand = "";
            if (messageParts.Length > 1)
                subCommand = messageParts[1].ToLower();

            IrcMessage response = new IrcMessage(message.location, conn.Connection.Me, "Response");
            response.type = Api.Irc.IrcReference.MessageType.ChannelMessage;

            Console.WriteLine("Handling command: " + command);

            switch (command) {
                case "exts":
                    switch (messageParts.Length) {
                        case 1:
                            response.message = "Invalid Parameters. Format: !exts [command]";
                            conn.Connection.SendMessage(response);
                            break;

                        case 2:
                            switch (subCommand.ToLower()) {
                                case "list":
                                    response.message = "Gathering data. May take a minute.";
                                    conn.Connection.SendMessage(response);

                                    ExtensionReference[] exts = GetServerExtensions(conn.Identifier);

                                    // TODO: Finish this
                                    break;

                                default:
                                    response.message = "Unknown command.";
                                    conn.Connection.SendMessage(response);
                                    break;
                            }

                            break;

                        case 3:
                            switch (subCommand.ToLower()) {

                                default:

                                    break;

                            }

                            break;
                    }

                    break;

                case "version":
                    response.type = Api.Irc.IrcReference.MessageType.ChannelAction;
                    response.message = "is currently running version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    conn.Connection.SendMessage(response);
                    break;


                case "raw":
                    if (conn.IsBotOperator(message.sender.nickname.ToLower())) {
                        string rawCommand = message.message.Split(new char[] { ' ' }, 2)[1];
                        conn.Connection.SendRaw(rawCommand);
                    } else {
                        response.message = "You are not a bot operator. No permission to execute raw commands.";
                        conn.Connection.SendMessage(response);
                    }
                    break;

                default:

                    

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
                Guid newExtGuid = Guid.NewGuid();

                ExtensionReference suite = new ExtensionReference();
                suite.Identifier = newExtGuid;
                suite.Socket = s;

                Extensions.Add(newExtGuid, suite);

                Console.WriteLine("[Extensions System] Connected extension.");

                s.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, RecieveDataCallback, s);

                // Why is this null again?
                Connection.BeginAccept(AcceptConnection, null);

                byte[] data = new byte[33];
                
                data[0] = (byte)Extension.ResponseCodes.Activation;
                Identifier.ToByteArray().CopyTo(data, 1);
                suite.Identifier.ToByteArray().CopyTo(data, 17);

                Extensions[suite.Identifier].Send(data);
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

            catch (SocketException se) {
                RemoveBySocket(recievedOn, "Extension crashed.");
            }
        }
        #endregion

        protected void HandleIncomingData(Socket sock, byte[] data) {
            Extension.ResponseCodes code = (Extension.ResponseCodes)data[0];
            byte[] guidBytes = new byte[16];
            Array.Copy(data, 1, guidBytes, 0, 16);

            Guid origin = new Guid(guidBytes);

            // Get the extension suite off the returned Identifier first
            ExtensionReference suite = Extensions[origin];

            #region Handle Code Response
            switch (code) {

                case Extension.ResponseCodes.Activation:

                    break;

                case Extension.ResponseCodes.ConnectionComplete:

                    break;

                case Extension.ResponseCodes.ExtensionPermissions:
                    byte[] permissions = new byte[data.Length - 17];
                    Array.Copy(data, 17, permissions, 0, permissions.Length);
                    foreach (byte perm in permissions) {
                        switch ((Extension.Permissions)perm) {

                            case Extension.Permissions.HandleUserEvent:
                                // bot.OnUserEvent += suite.HandleUserEvent;
                                break;

                            case Extension.Permissions.HandleCommand:

                                // TODO: Change to send command request back to socket
                                foreach (IrcBot bot in bots.Values) {
                                    bot.OnCommandRecieved += suite.HandleCommandRecieved;
                                }

                                break;

                            default:

                                break;

                        }
                    }
                    break;

                case Extension.ResponseCodes.ExtensionRemove:
                    sock.Shutdown(SocketShutdown.Both);
                    sock.Close();
                    return;

                case Extension.ResponseCodes.Message:
                    Array.Copy(data, 17, guidBytes, 0, 16);
                    Guid destination = new Guid(guidBytes);

                    IrcReference.MessageType type = (IrcReference.MessageType) data[34];
                    byte[] messageBytes = new byte[data.Length - 34];
                    Array.Copy(data, 34, messageBytes, 0, messageBytes.Length);

                    String messageData = Encoding.UTF8.GetString(messageBytes);
                    IrcMessage message = new IrcMessage("#channel", new IrcUser(), "");
                    message.type = type;
                    Match msg = ExtensionsReference.MessageResponseParser.Match(messageData);
                    message.message = msg.Groups["message"].Value;
                    message.location = msg.Groups["location"].Value;

                    // TODO: Fix send- destination wrong
                    try {
                        IrcBot bot = bots[destination];
                        bot.Connection.SendMessage(message);
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
                ext.Value.Send(new byte[] { (byte)Extension.ResponseCodes.ExtensionRemove });
                ext.Value.Socket.Shutdown(SocketShutdown.Both);
            }

            Extensions.Clear();
            Connection.Close();
        }

        internal ExtensionReference[] GetServerExtensions(Guid id) {
            return new ExtensionReference[0];
        }
        #endregion

    }
}

