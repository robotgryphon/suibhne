using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using Nini.Config;
using System.Reflection;

namespace Ostenvighx.Suibhne.Extensions {

    /// <summary>
    /// An extension suite holds a filename to an extension executable, the IDs of the extensions in
    /// the executable suite, and any information about the suite. (Author, Version, etc.)
    /// </summary>
    public abstract class Extension {

        public Guid Identifier;

        protected Socket conn;
        protected byte[] buffer;
        public Boolean Connected {
            get;
            protected set;
        }

        
        public delegate void CommandHandler(Extension e, Guid origin, string sender, string args);
        public delegate void ExtensionEvent(Extension e);

        protected Dictionary<Guid, CommandHandler> Commands;

        public event ExtensionEvent OnServerDisconnect;
        public event ExtensionEvent OnExtensionExit;

        public Extension() {
            this.buffer = new byte[2048];
            this.conn = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.Connected = false;           

            // TODO: Verify registration of commands in routing table here
            this.Commands = new Dictionary<Guid, CommandHandler>();
        }

        public void Start() {
            LoadConfig();
            MapCommandMethods();
            Connect();
        }

        public void LoadConfig() {
            IniConfigSource config = new IniConfigSource(Environment.CurrentDirectory + "/extension.ini");

            if (File.Exists(Environment.CurrentDirectory + @"\extension")) {

                BinaryReader file = new BinaryReader(File.OpenRead(Environment.CurrentDirectory + @"\extension"));

                try {
                    file.ReadString(); // Get past extension name - not used here.
                    this.Identifier = new Guid(file.ReadBytes(16));
                }

                catch (Exception) {

                }

                file.Close();

            } else {

                // Terminate, extension not installed properly

            }
        }

        private void MapCommandMethods() {
            // Get all possible command handler methods and create a mapping dictionary

            Dictionary<String, CommandHandler> methodMap = new Dictionary<string, CommandHandler>();

            MethodInfo[] definedMethods = this.GetType().GetMethods();
            foreach (MethodInfo method in definedMethods) {
                Object[] attrs = method.GetCustomAttributes(typeof(CommandHandlerAttribute), false);
                foreach (Object attr in attrs) {
                    if (attr.GetType() == typeof(CommandHandlerAttribute)) {
                        CommandHandlerAttribute handler = (CommandHandlerAttribute)attr;
                        Console.WriteLine("Got command handler: " + handler.Name + " (maps to " + method.Name + ")");

                        // TODO: Add in validation here for valid CommandHandler delegate
                        method.GetParameters();
                        CommandHandler methodDelegate = (CommandHandler)Delegate.CreateDelegate(typeof(CommandHandler), null, method);

                        methodMap.Add(handler.Name, methodDelegate);
                    }
                }
            }

            if (File.Exists(Environment.CurrentDirectory + @"\extension")) {

                FileStream file = File.OpenRead(Environment.CurrentDirectory + @"\extension");

                BinaryReader br = new BinaryReader(file);

                br.ReadString();        // Get past extension name
                br.ReadBytes(16);       // Get past extension identifier

                short methods = br.ReadInt16();
                for (int methodNumber = 1; methodNumber < methods + 1; methodNumber++) {
                    String methodName = br.ReadString();
                    byte[] guid = br.ReadBytes(16);
                    Guid g = new Guid(guid);

                    if (methodMap.ContainsKey(methodName)) {
                        this.Commands.Add(g, methodMap[methodName]);
                    }

                }

            }
        }

        public virtual string GetExtensionName() {
            return ((AssemblyTitleAttribute)this.GetType().Assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), true)[0]).Title.ToString();
        }

        public virtual string GetExtensionAuthor() {
            return ((AssemblyCompanyAttribute)this.GetType().Assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), true)[0]).Company.ToString();
        }

        public virtual string GetExtensionVersion() {
            return this.GetType().Assembly.GetName().Version.ToString();
        }

        public virtual void Connect() {

            try {
                Console.WriteLine("Starting conn");
                conn.BeginConnect("127.0.0.1", 6700, ConnectedCallback, conn);
            }

            catch (Exception e) {
                Console.WriteLine("Failed to start extension.");
                Console.WriteLine(e);
                if(this.OnServerDisconnect != null)
                    OnServerDisconnect(this);
            }

        }

        protected void ConnectedCallback(IAsyncResult result) {
            conn = (Socket)result.AsyncState;
            try {
                conn.EndConnect(result);
                Console.WriteLine("Conn finished");
                Connected = true;
                conn.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, RecieveDataCallback, conn);

                SendBytes(Responses.Activation, new byte[0]);

            }

            catch (SocketException se) {
                Console.WriteLine("Socket failed. Extension not started.");
                Console.WriteLine(se);

                if (this.OnExtensionExit != null)
                    OnExtensionExit(this);
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

                catch (SocketException se) {
                    // Handle socket suddenly shutting down. This is usually when main application quits for some reason.
                    if (this.OnServerDisconnect != null) {
                        OnServerDisconnect(this);
                        this.conn.Close();
                        this.conn.Dispose();
                    }
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
                if (data.Length > 17)
                    additionalData = Encoding.UTF8.GetString(data, 17, data.Length - 17);

                Console.WriteLine((Responses)data[0]);
                switch ((Responses)data[0]) {
                    case Responses.Activation:
                        // Connect suite name into bytes for response, then prepare response
                        byte[] nameAsBytes = Encoding.UTF8.GetBytes(GetExtensionName());
                        SendBytes(Responses.Details, nameAsBytes);

                        // TODO: Handle command validation here

                        break;

                    case Responses.Details:
                        string response =
                            "[" + Identifier + "] " +
                            GetExtensionName() + " (v. " + GetExtensionVersion() + ")" +
                            " developed by " + string.Join(", ", GetExtensionAuthor());

                        String[] messageParts = additionalData.Split(new char[] { ' ' }, 2);
                        String messageLocation = messageParts[0];
                        String messageSender = messageParts[1];

                        byte[] rawMessage = Extension.PrepareMessage(Identifier, origin, (byte) Ostenvighx.Suibhne.Networks.Base.Reference.MessageType.PublicMessage, this.GetExtensionName().Replace(" ", "_"), response);
                        SendBytes(Responses.Message, rawMessage);

                        break;

                    case Responses.Command:
                        if (data.Length > 33) {
                            guidBytes = new byte[16];
                            Array.Copy(data, 17, guidBytes, 0, 16);
                            Guid commandID = new Guid(guidBytes);

                            byte[] commandInfoBytes = new byte[data.Length - 33];
                            Array.Copy(data, 33, commandInfoBytes, 0, commandInfoBytes.Length);
                            String commandInfo = Encoding.UTF8.GetString(commandInfoBytes);
                            if (Commands.ContainsKey(commandID)) {
                                string[] cdata = commandInfo.Split(new char[] { ' ' }, 2);
                                Commands[commandID].Invoke(this, origin, cdata[0], cdata[1]);
                            }
                        } else {
                            Console.WriteLine("Invalid command string. Need identifier, at least.");
                        }
                        break;

                    case Responses.Help:
                        if (data.Length > 33) {
                            guidBytes = new byte[16];
                            Array.Copy(data, 17, guidBytes, 0, 16);
                            Guid commandID = new Guid(guidBytes);

                            byte[] commandInfoBytes = new byte[data.Length - 33];
                            Array.Copy(data, 33, commandInfoBytes, 0, commandInfoBytes.Length);
                            String commandInfo = Encoding.UTF8.GetString(commandInfoBytes);
                            if (Commands.ContainsKey(commandID)) {
                                string[] cdata = commandInfo.Split(new char[] { ' ' }, 3);
                                MethodInfo method = Commands[commandID].Method;

                                Object[] attrs = method.GetCustomAttributes(typeof(HelpAttribute), false);
                                foreach (Object attr in attrs) {
                                    if (attr.GetType() == typeof(HelpAttribute)) {
                                        HelpAttribute handler = (HelpAttribute)attr;
                                        SendMessage(origin, Networks.Base.Reference.MessageType.PublicMessage, handler.HelpText);
                                    }
                                }
                            }
                        }
                        break;

                    case Responses.Message:
                        HandleIncomingMessage(data);
                        break;


                    case Responses.Remove:
                        conn.Shutdown(SocketShutdown.Both);
                        conn.Close();
                        Connected = false;

                        if (this.OnExtensionExit != null) {
                            OnExtensionExit(this);
                        }
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
        public virtual void SendBytes(Responses code, byte[] data) {
            byte[] dataToSend = new byte[17 + data.Length];
            dataToSend[0] = (byte)code;
            Array.Copy(Identifier.ToByteArray(), 0, dataToSend, 1, 16);
            Array.Copy(data, 0, dataToSend, 17, data.Length);

            Console.WriteLine("Sending Data");
            conn.Send(dataToSend);
        }

        public static byte[] PrepareMessage(Guid origin, Networks.Base.Message message) {
            return PrepareMessage(
                origin,
                message.locationID,
                (byte)message.type,
                message.sender.DisplayName,
                message.message);
        }

        /// <summary>
        /// Deprecated. You should use PrepareMessage(Guid origin, Message message) instead.
        /// </summary>
        public static byte[] PrepareMessage(Guid origin, Guid destination, byte type, String sender, String message) {
            byte[] messageAsBytes = Encoding.UTF8.GetBytes(sender + " " + message);
            byte[] rawMessage = new byte[34 + messageAsBytes.Length];

            rawMessage[0] = (byte)Responses.Message;

            Array.Copy(origin.ToByteArray(), 0, rawMessage, 1, 16);
            Array.Copy(destination.ToByteArray(), 0, rawMessage, 17, 16);

            rawMessage[33] = type;
            Array.Copy(messageAsBytes, 0, rawMessage, 34, messageAsBytes.Length);

            return rawMessage;
        }

        public static Networks.Base.Message ParseMessage(byte[] data) {
            Guid origin;
            return ParseMessage(data, out origin);
        }

        public static Networks.Base.Message ParseMessage(byte[] data, out Guid origin) {
            byte[] guidBytes = new byte[16];

            Array.Copy(data, 1, guidBytes, 0, 16);
            origin = new Guid(guidBytes);

            guidBytes = new byte[16];

            Array.Copy(data, 17, guidBytes, 0, 16);

            Networks.Base.Message message = new Networks.Base.Message(new Guid(guidBytes), new Networks.Base.User(), "");
            message.type = (Ostenvighx.Suibhne.Networks.Base.Reference.MessageType) data[33];

            byte[] messageBytes = new byte[data.Length - 34];
            Array.Copy(data, 34, messageBytes, 0, messageBytes.Length);
            message.message = Encoding.UTF8.GetString(messageBytes);

            Match messageMatch = Reference.MessageResponseParser.Match(message.message);
            if (messageMatch.Success) {
                message.sender.DisplayName = messageMatch.Groups["sender"].Value;
                message.message = messageMatch.Groups["message"].Value;
            } else {
                message.sender.DisplayName = "Unknown";
                message.message = "Message";
            }

            return message;
        }

        protected virtual void HandleIncomingMessage(byte[] data) {
            Guid origin;
            Networks.Base.Message msg = ParseMessage(data, out origin);

            Console.WriteLine("Origin [server]: " + origin);
            Console.WriteLine("Destination: " + msg.locationID);

            Console.WriteLine("Recieved message from " + msg.sender.DisplayName + ": " + msg.message);
        }

        public void SendMessage(Networks.Base.Message message) {
            SendMessage(message.locationID, message.type, message.message);
        }

        public void SendMessage(Guid destination, Networks.Base.Reference.MessageType type, String message) {
            byte[] rawMessage = PrepareMessage(this.Identifier, destination, (byte)type, this.GetExtensionName().Replace(' ', '_'), message);
            conn.Send(rawMessage);
        }


    }


}

