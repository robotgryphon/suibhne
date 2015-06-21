using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Ostenvighx.Suibhne.Extensions {
    public class ExtensionServer {

        protected Socket Connection;
        protected byte[] Buffer;

        public delegate void DataEvent(Socket sock, byte[] data);
        public event DataEvent OnDataRecieved;

        public delegate void ExtensionDataEvent(ExtensionMap ext, byte[] data);
        public event ExtensionDataEvent OnExtDataRecieved;

        public ExtensionServer() {
            this.Connection = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            this.Buffer = new byte[1024];
        }

        public void Start() {
            Core.Log("Setting up server..", LogType.EXTENSIONS);

            Connection.Bind(new IPEndPoint(IPAddress.Any, 6700));
            Connection.Listen(5);

            Connection.BeginAccept(new AsyncCallback(AcceptConnection), null);
            Core.Log("Server setup complete. Extensions system ready.", LogType.EXTENSIONS);
            Console.WriteLine();
        }

        public void Stop() { }

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

                if (recievedAmount <= 0) {
                    // TODO: HandleSocketDisconnect();
                    recievedOn.Shutdown(SocketShutdown.Both);
                    return;
                }

                byte[] btemp = new byte[recievedAmount];
                Array.Copy(Buffer, btemp, recievedAmount);

                if (this.OnDataRecieved != null)
                    OnDataRecieved(recievedOn, btemp);

                recievedOn.BeginReceive(Buffer, 0, Buffer.Length, SocketFlags.None, RecieveDataCallback, recievedOn);
            }

            catch (SocketException) {
                // RemoveBySocket(recievedOn, "Extension crashed.");
            }
        }


    }
}
