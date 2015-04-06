using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
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

            Details = 2,

            Permissions = 3,

            /// <summary>
            /// Called when bot or extension requests a reactivation
            /// of extension.
            /// </summary>
            Restart = 4,

            /// <summary>
            /// Called when a bot requests an extension be disabled 
            /// at runtime.
            /// </summary>
            Remove = 5,

            Commands = 6,

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
        public Boolean Connected {
            get;
            protected set;
        }

        public byte[] PermissionList {
            get;
            protected set;
        }

        protected Dictionary<String, Guid> CommandsList;

        public Extension() {
            this.Name = "Extension";
            this.Authors = new String[] { "Unknown Author" };
            this.Version = "0.0.1";
            this.buffer = new byte[2048];
            this.conn = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.PermissionList = new byte[0];
            this.Connected = false;
            this.CommandsList = new Dictionary<String, Guid>();
        }

        public virtual void Connect() {

            try {
                Console.WriteLine("STarting conn");
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
                Console.WriteLine("Conn finished");
                Connected = true;
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

                byte[] guidBytes = new byte[16];
                Array.Copy(data, 1, guidBytes, 0, 16);
                Guid origin = new Guid(guidBytes);

                String additionalData = "";
                if(data.Length > 17)
                    additionalData = Encoding.UTF8.GetString(data, 17, data.Length - 17);

                switch ((ResponseCodes)data[0]) {
                    case ResponseCodes.Activation:
                        // Allocate space to keep GUID as bytes in, for processing
                        Array.Copy(data, 17, guidBytes, 0, 16);

                        // Store Identifier for later use
                        this.Identifier = new Guid(guidBytes);

                        Console.WriteLine("Got identifier: " + Identifier);

                        // Connect suite name into bytes for response, then prepare response
                        byte[] nameAsBytes = Encoding.UTF8.GetBytes(Name);
                        SendBytes(ResponseCodes.Details, nameAsBytes);
                        SendBytes(ResponseCodes.Permissions, PermissionList);

                        Console.WriteLine("Sending command data..");
                        foreach (KeyValuePair<String, Guid> command in CommandsList) {
                            Console.WriteLine("Registering '{0}' using id '{1}'", command.Key, command.Value);
                            SendBytes(ResponseCodes.Commands, Encoding.UTF8.GetBytes(command.Key + " " + command.Value));
                        }

                        break;

                    case ResponseCodes.Details:
                        string response = 
                            "[" + Reference.ColorPrefix + "05" + Identifier + Reference.Normal + "] " + 
                            Name + Reference.ColorPrefix + "02 (v. " + Version + ")" + Reference.Normal + 
                            " developed by " + Reference.ColorPrefix + "03" + string.Join(", ", Authors);

                        String[] messageParts = additionalData.Split(new char[] { ' ' }, 2);
                        String messageLocation = messageParts[0];
                        String messageSender = messageParts[1];

                        SendMessage(origin, Reference.MessageType.ChannelMessage, messageLocation, response);

                        break;

                    case ResponseCodes.Message:
                        HandleIncomingMessage(data);
                        break;


                    case ResponseCodes.Remove:
                        conn.Shutdown(SocketShutdown.Both);
                        conn.Close();
                        Connected = false;
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
            dataToSend[0] = (byte)code;
            Array.Copy(Identifier.ToByteArray(), 0, dataToSend, 1, 16);
            Array.Copy(data, 0, dataToSend, 17, data.Length);

            Console.WriteLine("Sending Data");
            conn.Send(dataToSend);
        }

        public static byte[] PrepareMessage(Guid origin, Guid destination, byte type, String location, String sender, String message) {
            byte[] messageAsBytes = Encoding.UTF8.GetBytes(location + " " + sender + " " + message);
            byte[] rawMessage = new byte[34 + messageAsBytes.Length];

            rawMessage[0] = (byte)ResponseCodes.Message;

            Array.Copy(origin.ToByteArray(), 0, rawMessage, 1, 16);
            Array.Copy(destination.ToByteArray(), 0, rawMessage, 17, 16);

            rawMessage[33] = type;
            Array.Copy(messageAsBytes, 0, rawMessage, 34, messageAsBytes.Length);

            return rawMessage;
        }

        public static void ParseMessage(byte[] data, out Guid origin, out Guid destination, out byte type, out String location, out String sender, out String message) {
            byte[] guidBytes = new byte[16];
            
            Array.Copy(data, 1, guidBytes, 0, 16);
            origin = new Guid(guidBytes);

            guidBytes = new byte[16];

            Array.Copy(data, 17, guidBytes, 0, 16);
            destination = new Guid(guidBytes);

            type = data[33];

            byte[] messageBytes = new byte[data.Length - 34];
            Array.Copy(data, 34, messageBytes, 0, messageBytes.Length);
            String messageString = Encoding.UTF8.GetString(messageBytes);

            Match messageMatch = Reference.MessageResponseParser.Match(messageString);
            if (messageMatch.Success) {
                location = messageMatch.Groups["location"].Value;
                sender = messageMatch.Groups["sender"].Value;
                message = messageMatch.Groups["message"].Value;
            } else {
                location = "#channel";
                sender = "Unknown";
                message = "Message";
            }
        }

        protected void SendMessage(Guid destination, Reference.MessageType type, String location, String message) {
            byte[] rawMessage = PrepareMessage(Identifier, destination, (byte) type, location, Name.Replace(' ', '_'), message);
            conn.Send(rawMessage);            
        }

        protected virtual void HandleIncomingMessage(byte[] data) {

            
 
            Guid destination = this.Identifier;
            byte type = 1;
            String location, nickname, message;
            Guid origin;
            ParseMessage(
                data,
                out origin,
                out destination,
                out type,
                out location,
                out nickname,
                out message);

            Console.WriteLine("Origin [server]: " + origin);
            Console.WriteLine("Destination: " + destination);

            Console.WriteLine("Recieved message from " + nickname + ": " + message);
        }
    }
}

