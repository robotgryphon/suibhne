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
    /// It goes through the extensions directory defined in the bot configuration and
    /// searches through directories for the extension INI files.
    /// </summary>
    public class ExtensionSystem {

        public Guid Identifier;

        protected Dictionary<Guid, IrcBot> bots;

        protected Dictionary<Guid, ExtensionReference> Extensions;

        protected Dictionary<String, Guid> CommandMapping;

        public Socket Connection;

        public static byte[] spaceBytes = Encoding.UTF8.GetBytes(" ");

        protected byte[] Buffer;

        protected DateTime StartTime;

        public ExtensionSystem() {
            this.bots = new Dictionary<Guid, IrcBot>();
            this.Identifier = Guid.NewGuid();

            this.CommandMapping = new Dictionary<string, Guid>();
            this.Extensions = new Dictionary<Guid, ExtensionReference>();
            this.Connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.Extensions = new Dictionary<Guid, ExtensionReference>();

            this.Buffer = new byte[1024];

            this.StartTime = DateTime.Now;

            StartServer();
        }

        #region Registry
        public void AddBot(IrcBot bot) {
            if (!this.bots.ContainsKey(bot.Identifier))
                bots.Add(bot.Identifier, bot);
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
                                            switch (subCommand.ToLower()) {
                                                case "list":
                                                    response.message = "Gathering data for global extension list. May take a minute.";
                                                    conn.SendMessage(response);

                                                    ExtensionReference[] exts = GetServerExtensions(conn.Identifier);

                                                    if (exts.Length > 0) {
                                                        byte[] originBytes = Encoding.UTF8.GetBytes(message.sender.nickname + " " + message.location);
                                                        byte[] request = new byte[17 + originBytes.Length];
                                                        request[0] = (byte)Extension.ResponseCodes.ExtensionDetails;
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
                                            switch (subCommand.ToLower()) {

                                                case "add":

                                                    break;

                                                case "res":

                                                    break;

                                                case "rem":

                                                    break;

                                                default:

                                                    break;

                                            }

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
                    // TODO: Handle command through commands registry
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

            // Get the extension suite off the returned Identifier first
            try {

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
                ext.Value.Send(new byte[] { (byte)Extension.ResponseCodes.ExtensionRemove });
                ext.Value.Socket.Shutdown(SocketShutdown.Both);
            }

            Extensions.Clear();
            Connection.Close();
        }

        // TODO: Start tracking which extensions are enabled on which server

        internal ExtensionReference[] GetServerExtensions(Guid id) {
            return new ExtensionReference[0];

        }
        #endregion

    }
}

