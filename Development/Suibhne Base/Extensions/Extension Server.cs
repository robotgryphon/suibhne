using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

using Raindrop.Api.Irc;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;

namespace Raindrop.Suibhne.Extensions {
    public class ExtensionServer {

        protected IrcBot bot;
        public Socket Connection;

        public static byte[] spaceBytes = Encoding.UTF8.GetBytes(" ");

        public Dictionary<Guid, ExtensionReference> Extensions {
            get;
            protected set;
        }

        protected byte[] Buffer;

        public ExtensionServer(IrcBot bot) {
            this.bot = bot;
            this.Connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.Extensions = new Dictionary<Guid, ExtensionReference>();

            this.Buffer = new byte[1024];

            Setup();
        }

        protected void Setup() {
            Console.WriteLine("[Extensions System] Setting up server..");

            Connection.Bind(new IPEndPoint(IPAddress.Any, 6700));
            Connection.Listen(5);

            Connection.BeginAccept(new AsyncCallback(AcceptConnection), null);
            Console.WriteLine("[Extensions System] Server setup complete. Extensions system ready.");
        }

        internal void Broadcast(String data) {
            foreach (KeyValuePair<Guid, ExtensionReference> suite in Extensions)
                suite.Value.SendString(data);
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

                byte[] idBytes = suite.Identifier.ToByteArray();
                byte[] data = new byte[1 + idBytes.Length + spaceBytes.Length];
                data[0] = (byte)Extension.ResponseCodes.Activation;
                idBytes.CopyTo(data, 1);
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


        // TODO: Handle extension crash/force close gracefully.
        protected void HandleIncomingData(Socket sock, byte[] data) {
            Extension.ResponseCodes code = (Extension.ResponseCodes)data[0];
            byte[] idBytes = new byte[16];
            Array.Copy(data, 1, idBytes, 0, 16);

            Guid suiteID = new Guid(idBytes);

            byte[] otherData = new byte[data.Length - 17];
            Array.Copy(data, 17, otherData, 0, data.Length - 17);
            String otherDataString = Encoding.UTF8.GetString(otherData);

            Console.WriteLine("[Extensions System] Recieved: " + otherDataString);

            // Get the extension suite off the returned Identifier first
            ExtensionReference suite = Extensions[suiteID];

            #region Handle Code Response
            switch (code) {

                case Extension.ResponseCodes.Activation:

                    break;

                case Extension.ResponseCodes.ConnectionComplete:

                    break;

                case Extension.ResponseCodes.ExtensionPermissions:
                    foreach (byte perm in otherData) {
                        switch ((Extension.Permissions)perm) {

                            case Extension.Permissions.HandleUserEvent:
                                // bot.OnUserEvent += suite.HandleUserEvent;
                                break;

                            case Extension.Permissions.HandleCommand:

                                // TODO: Change to send command request back to socket
                                bot.OnCommandRecieved += suite.HandleCommandRecieved;
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
                    // Required info: connid, location, type, message
                    byte connid = otherData[0];
                    IrcReference.MessageType type = (IrcReference.MessageType) otherData[1];

                    String messageData = Encoding.UTF8.GetString(otherData, 2, otherData.Length - 2);
                    IrcMessage message = new IrcMessage("#channel", new IrcUser(), "");
                    string[] messageParts = messageData.Split(new char[] { ' ' }, 2);
                    message.type = type;
                    message.location = messageParts[0];
                    message.sender.nickname = suite.Name;
                    message.message = messageParts[1];
                    

                    IrcConnection conn = bot.Connections[connid].Connection;
                    conn.SendMessage(message);
                    break;

                default:
                    // Unknown response

                    break;

            }
            #endregion
        }

        internal void SendToExtension(Guid extID, byte connID, IrcMessage msg) {
            if (Extensions.ContainsKey(extID))
                Extensions[extID].SendMessage(connID, msg);
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

        internal void ShowDetails(byte connID, String sender, String location) {

            if (Extensions.Count > 0) {
                byte[] prefix = Extension.GetLocalizedPrefix(connID, Extension.ResponseCodes.ExtensionDetails, sender, location);
                foreach (KeyValuePair<Guid, ExtensionReference> ext in Extensions) {
                    ext.Value.Send(prefix);
                    Thread.Sleep(500);
                }
            } else {
                IrcConnection conn = bot.Connections[connID].Connection;
                conn.SendMessage(new IrcMessage(location, conn.Me, "No extensions active."));
            }
        }
    }
}
