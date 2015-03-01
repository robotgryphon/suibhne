using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Raindrop.Suibhne.Extensions {

    /// <summary>
    /// An extension suite holds a filename to an extension executable, the IDs of the extensions in
    /// the executable suite, and any information about the suite. (Author, Version, etc.)
    /// </summary>
    public abstract class Extension {

        public enum ResponseCodes : byte {
            /// <summary>
            /// Request for extension name, runtype, and permissions.
            /// </summary>
            Activation = 1,

            SuiteDetails = 2,

            ExtensionPermissions = 3,

            /// <summary>
            /// Called when bot or extension requests a reactivation
            /// of extension.
            /// </summary>
            ExtensionRestart = 4,

            /// <summary>
            /// Called when a bot requests an extension be disabled 
            /// at runtime.
            /// </summary>
            ExtensionRemove = 5,

            ExtensionCommands = 6,

            /// <summary>
            /// Request for connection details.
            /// </summary>
            ConnectionDetails = 10,

            /// <summary>
            /// Connection initialized and starting up.
            /// Includes a connection ID for tracking.
            /// </summary>
            ConnectionStart = 11,

            /// <summary>
            /// Connection finished connecting.
            /// Includes a connection ID for tracking.
            /// </summary>
            ConnectionComplete = 12,

            /// <summary>
            /// Connection starting disconnect.
            /// Includes a connection ID for tracking.
            /// </summary>
            ConnectionEnding = 13,

            /// <summary>
            /// Connection finished disconnecting.
            /// Includes a connection ID for tracking.
            /// </summary>
            ConnectionStopped = 14,

            Message = 20
        };

        public enum Permissions : byte {
            HandleConnection,
            HandleUserEvent,
            HandleCommand
        }

        public String Name { get; protected set; }
        public Guid Identifier;

        /// <summary>
        /// The names of the extension suite authors.
        /// </summary>
        /// <value>The authors of the extension suite.</value>
        public String[] Authors { get; protected set; }

        /// <summary>
        /// The version number of the extension suite.
        /// This should typically be done in Major.Minor.Patch format. (Such as 1.0.3)
        /// Default is 0.0.1.
        /// </summary>
        /// <value>The version of the extension suite.</value>
        public String Version { get; protected set; }

        protected Socket conn;
        protected byte[] buffer;
        
        public byte[] PermissionList {
            get;
            protected set;
        }

        public Extension() {
            this.Name = "Extension";
            this.Authors = new String[] { "Unknown Author" };
            this.Version = "0.0.1";
            this.buffer = new byte[2048];
            this.conn = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.PermissionList = new byte[0];
        }

        public virtual void Connect() {

            try {
                conn.BeginConnect("127.0.0.1", 6700, ConnectedCallback, conn);
            }

            catch (Exception e) {
                Console.WriteLine("Failed to start extension.");
                Console.WriteLine(e);
            }

        }

        protected void ConnectedCallback(IAsyncResult result) {
            conn = (Socket)result.AsyncState;
            try {
                conn.EndConnect(result);

                conn.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, RecieveDataCallback, conn);
            }

            catch (SocketException se) {
                Console.WriteLine("Socket failed. Extension not started.");
                Console.WriteLine(se);
            }
        }

        protected void RecieveDataCallback(IAsyncResult result) {

            Socket recievedOn = (Socket)result.AsyncState;
            if (recievedOn.Connected) {
                try {
                    int recievedAmount = recievedOn.EndReceive(result);

                    byte[] btemp = new byte[recievedAmount];
                    Array.Copy(buffer, btemp, recievedAmount);

                    HandleIncomingData(btemp);

                    recievedOn.BeginReceive(this.buffer, 0, buffer.Length, SocketFlags.None, RecieveDataCallback, recievedOn);
                }

                catch (Exception e) {
                    Console.WriteLine(e);
                }
            } else {
                // Exit
            }
        }

        protected virtual void HandleIncomingData(byte[] data) {
            try {
                // Get data from buffer, handle it


                switch ((ResponseCodes)data[0]) {
                    case ResponseCodes.Activation:
                        // Allocate space to keep GUID as bytes in, for processing
                        byte[] idBytes = new byte[16];
                        Array.Copy(data, 1, idBytes, 0, 16);

                        // Store Identifier for later use
                        this.Identifier = new Guid(idBytes);

                        // Connect suite name into bytes for response, then prepare response
                        byte[] nameAsBytes = Encoding.ASCII.GetBytes(Name);
                        SendBytes(ResponseCodes.SuiteDetails, nameAsBytes);
                        SendBytes(ResponseCodes.ExtensionPermissions, PermissionList);
                        break;

                    case ResponseCodes.Message:
                            byte connID = data[1];
                            byte messageType = data[2];
            
                            String messageValue = Encoding.UTF8.GetString(data, 3, data.Length - 3);
                            String[] messageParts = messageValue.Split(new char[] { ' ' }, 3);
                            String location = messageParts[0];
                            String sender = messageParts[1];
                            String message = messageParts[2].Substring(1);
                            
                            HandleIncomingMessage(connID, messageType, sender, location, message);
                        break;

                }

            }

            catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Send a series of bytes to the socket, prefixing it with a response code
        /// and the extension identifier.
        /// </summary>
        /// <param name="code"></param>
        /// <param name="data"></param>
        protected virtual void SendBytes(ResponseCodes code, byte[] data) {
            byte[] dataToSend = new byte[17 + data.Length];
            dataToSend[0] = (byte) code;
            Array.Copy(Identifier.ToByteArray(), 0, dataToSend, 1, 16);
            Array.Copy(data, 0, dataToSend, 17, data.Length);

            conn.Send(dataToSend);
        }

        protected void SendMessage(byte connID, byte messageType, String location, String message) {
            // Format: connID messageType location MESSAGE
            byte[] messageAsBytes = Encoding.UTF8.GetBytes(location + " " + message);
            byte[] rawMessage = new byte[2 + messageAsBytes.Length];

            rawMessage[0] = connID;
            rawMessage[1] = messageType;
            Array.Copy(messageAsBytes, 0, rawMessage, 2, messageAsBytes.Length);

            SendBytes(ResponseCodes.Message, rawMessage);
        }

        protected virtual void HandleIncomingMessage(byte connID, byte messageType, String sender, String location, String message) {
            Console.WriteLine("Recieved message from " + sender + ": " + message);
        }
    }
}

