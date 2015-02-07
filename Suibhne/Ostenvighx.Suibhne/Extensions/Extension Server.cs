using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

using Ostenvighx.Api.Irc;
using Ostenvighx.Suibhne.Core;
using System.Net;
using System.Threading;

namespace Ostenvighx.Suibhne.Extensions {
    public class ExtensionServer {

        protected IrcBot bot;
        public Socket Connection;

        protected List<Socket> Clients;
        protected byte[] Buffer;

        protected Thread workThread;

        public ExtensionServer(IrcBot bot) {
            this.bot = bot;
            this.Connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.Clients = new List<Socket>();
            this.Buffer = new byte[1024];

            this.workThread = new Thread(new ThreadStart(ThreadLoop));
            workThread.Start();

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
            for (int i = 0; i < Clients.Count; i++) {
                SendText(Clients[i], data);
            }
        }

        public void SendText(Socket sock, String data) {
            byte[] buff = Encoding.ASCII.GetBytes(data);
            try {
                sock.Send(buff);
            }

            catch (SocketException so) {
                Clients.Remove(sock);
            }
        }

        #region Socket Handling Callbacks
        protected void AcceptConnection(IAsyncResult result) {
            Socket s = Connection.EndAccept(result);
            Clients.Add(s);

            Console.WriteLine("[Extensions System] Connected client.");

            s.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, RecieveDataCallback, s);
            Connection.BeginAccept(AcceptConnection, null);

            SendText(s, "ext.acceptConnection " + Guid.NewGuid());
        }

        protected void RecieveDataCallback(IAsyncResult result) {
            Socket recievedOn = (Socket) result.AsyncState;
            try {
                int recievedAmount = recievedOn.EndReceive(result);

                byte[] btemp = new byte[recievedAmount];
                Array.Copy(Buffer, btemp, recievedAmount);

                String text = Encoding.ASCII.GetString(btemp);
                Console.WriteLine("[Extensions System] Recieved: " + text);

                if (text.Trim().ToLower() == "ext.shutdown") {
                    recievedOn.Shutdown(SocketShutdown.Both);
                    recievedOn.Close();
                    Clients.Remove(recievedOn);
                    return;
                }

                recievedOn.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, RecieveDataCallback, recievedOn);
            }

            catch (SocketException se) {
                Clients.Remove(recievedOn);
                Console.WriteLine(se);
            }

            
        }
        #endregion

        protected void ThreadLoop() {
            while (true) {
                Thread.Sleep(5000);
            }
        }
    }
}
