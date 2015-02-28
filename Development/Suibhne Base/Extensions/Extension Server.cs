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

        public Dictionary<Guid, ExtensionSuiteReference> Extensions {
            get;
            protected set;
        }

        protected byte[] Buffer;

        public ExtensionServer(IrcBot bot) {
            this.bot = bot;
            this.Connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.Extensions = new Dictionary<Guid, ExtensionSuiteReference>();

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
            foreach (KeyValuePair<Guid, ExtensionSuiteReference> suite in Extensions)
                suite.Value.SendString(data);
        }

        #region Socket Handling Callbacks
        protected void AcceptConnection(IAsyncResult result) {
            Socket s = Connection.EndAccept(result);
            Guid newExtGuid = Guid.NewGuid();

            ExtensionSuiteReference suite = new ExtensionSuiteReference();
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
                Console.WriteLine(se);
            }


        }
        #endregion

        protected void HandleIncomingData(Socket sock, byte[] data) {
            Extension.ResponseCodes code = (Extension.ResponseCodes)data[0];
            byte[] idBytes = new byte[16];
            Array.Copy(data, 1, idBytes, 0, 16);

            Guid suiteID = new Guid(idBytes);

            String otherData = Encoding.ASCII.GetString(data, 17, data.Length - 17);
            
            Console.WriteLine("[Extensions System] Recieved: " + otherData);

            // Get the extension suite off the returned Identifier first
            ExtensionSuiteReference suite = Extensions[suiteID];

            switch (code) {

                case Extension.ResponseCodes.Activation:

                    break;

                case Extension.ResponseCodes.SuiteDetails:
                    suite.Name = otherData.Trim();
                    Console.WriteLine("Got extension suite name: " + suite.Name);

                    // TODO: Latch onto permissions here
                    break;

                case Extension.ResponseCodes.ConnectionComplete:

                    break;

                case Extension.ResponseCodes.ExtensionRemove:
                    sock.Shutdown(SocketShutdown.Both);
                    sock.Close();
                    return;

                case Extension.ResponseCodes.Message:
                    // Required info: connid, location, type, message
                    byte connid = 0;
                    String location = "#channel";
                    String message = "message";
                    
                    IrcConnection conn = bot.Connections[connid].Connection;
                    conn.SendMessage(new IrcMessage(location, conn.Me, message));
                    break;

                default:
                    // Unknown response
                    
                    break;

            }
        }

        public void SendToExtension(Guid extID, byte connID, IrcMessage msg) {
            if (Extensions.ContainsKey(extID))
                Extensions[extID].SendMessage(connID, msg);
        }
    }
}
