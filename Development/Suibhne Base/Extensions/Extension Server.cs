using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

using Raindrop.Api.Irc;
using Raindrop.Suibhne.Core;
using System.Net;
using System.Threading;
using System.Text.RegularExpressions;

namespace Raindrop.Suibhne.Extensions {
    public class ExtensionServer {

        protected IrcBot bot;
        public Socket Connection;

        public static byte[] spaceBytes = Encoding.ASCII.GetBytes(" ");

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

        public void Broadcast(String data) {
            foreach (KeyValuePair<Guid, ExtensionReference> suite in Extensions)
                suite.Value.SendString(data);
        }

        #region Socket Handling Callbacks
        protected void AcceptConnection(IAsyncResult result) {
            Socket s = Connection.EndAccept(result);
            Guid newExtGuid = Guid.NewGuid();

            ExtensionReference suite = new ExtensionReference();
            suite.Identifier = newExtGuid;
            suite.Socket = s;
            
            Extensions.Add(newExtGuid, suite);

            Console.WriteLine("[Extensions System] Connected extension.");

            s.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, RecieveDataCallback, s);
            Connection.BeginAccept(AcceptConnection, null);

            byte[] idBytes = suite.Identifier.ToByteArray();
            byte[] data = new byte[1 + idBytes.Length + spaceBytes.Length];
            data[0] = (byte)Extension.ResponseCodes.Activation;
            idBytes.CopyTo(data, 1);
            Extensions[suite.Identifier].Send(data);
        }

        protected void RecieveDataCallback(IAsyncResult result) {
            Socket recievedOn = (Socket)result.AsyncState;
            try {
                int recievedAmount = recievedOn.EndReceive(result);

                byte[] btemp = new byte[recievedAmount];
                Array.Copy(Buffer, btemp, recievedAmount);

                HandleIncomingData(recievedOn, btemp);

                recievedOn.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, RecieveDataCallback, recievedOn);
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
            String otherDataString = Encoding.ASCII.GetString(otherData);

            Console.WriteLine("[Extensions System] Recieved: " + otherDataString);

            // Get the extension suite off the returned Identifier first
            ExtensionReference suite = Extensions[suiteID];

            #region Handle Code Response
            switch (code) {

                case Extension.ResponseCodes.Activation:

                    break;

                case Extension.ResponseCodes.SuiteDetails:
                    suite.Name = otherDataString.Trim();
                    Console.WriteLine("Got extension suite name: " + suite.Name);
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

                                // TODO: Look into registered command dictionary first.
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
                    Reference.MessageType type = (Reference.MessageType) otherData[1];

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

        public void SendToExtension(Guid extID, byte connID, IrcMessage msg) {
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
    }
}
