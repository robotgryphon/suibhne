using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Linq;

using System.Reflection;
using Ostenvighx.Suibhne.Networks.Base;
using Newtonsoft.Json.Linq;

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

        
        public delegate void CommandHandler(Extension e, Networks.Base.Message msg);
        public delegate void ExtensionEvent(Extension e);

        protected Dictionary<Guid, CommandHandler> Commands;

        public event ExtensionEvent OnServerDisconnect;
        public event ExtensionEvent OnExtensionExit;

        public Extension() {
            this.buffer = new byte[2048];
            this.conn = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.Connected = false;           

            this.Commands = new Dictionary<Guid, CommandHandler>();
        }

        public void Start() {
            LoadConfig();

            Console.WriteLine("Loaded id: " + Identifier);
            MapCommandMethods();
            Connect();
        }

        public void LoadConfig() {
            String systemFile = Environment.CurrentDirectory + @"\extension.sns";
            if (File.Exists(systemFile)) {

                string encodedFile = File.ReadAllText(systemFile);
                string decodedFile = Encoding.UTF8.GetString(Convert.FromBase64String(encodedFile));
                JObject config = JObject.Parse(decodedFile);

                if (config == null)
                    return;

                this.Identifier = new Guid((String) config.GetValue("Identifier"));

            } else {

                // Terminate, extension not installed properly
                throw new FileNotFoundException("System information file not found. Please re-install the extension.");
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

                        ParameterInfo[] commandMethodParams = method.GetParameters();

                        if (commandMethodParams.Length != 2 || (
                            commandMethodParams[0].ParameterType != typeof(Extension) || commandMethodParams[1].ParameterType != typeof(Networks.Base.Message))
                        )
                            throw new Exception("Invalid method signature for command handler: " + method.DeclaringType.FullName + ":" + method.Name);

                        CommandHandler methodDelegate = (CommandHandler)Delegate.CreateDelegate(typeof(CommandHandler), null, method);
                        methodMap.Add(handler.Name, methodDelegate);
                    }
                }
            }

            String systemFile = Environment.CurrentDirectory + @"\extension.sns";
            if (File.Exists(systemFile)) {

                string encodedFile = File.ReadAllText(systemFile);
                string decodedFile = Encoding.UTF8.GetString(Convert.FromBase64String(encodedFile));
                JObject config = JObject.Parse(decodedFile);

                foreach(JProperty method in config["CommandHandlers"]) {
                    String methodName = method.Name;
                    Guid g = Guid.Parse((String) method.Value);

                    if (methodMap.ContainsKey(methodName)) {
                        Console.WriteLine("Got command mapping id for " + method.Name + ": " + method.Value);
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

                switch ((Responses)data[0]) {
                    case Responses.Activation:
                        // Connect suite name into bytes for response, then prepare response
                        byte[] nameAsBytes = Encoding.UTF8.GetBytes(GetExtensionName());
                        SendBytes(Responses.Details, nameAsBytes);

                        break;

                    case Responses.Details:
                        string response =
                            "[" + Identifier + "] " +
                            GetExtensionName() + " (v. " + GetExtensionVersion() + ")" +
                            " developed by " + string.Join(", ", GetExtensionAuthor());

                        String[] messageParts = additionalData.Split(new char[] { ' ' }, 2);
                        String messageLocation = messageParts[0];
                        String messageSender = messageParts[1];

                        SendMessage(new Message(origin, new User(), response));
                        break;

                    case Responses.Command:
                        if (data.Length > 33) {

                            Guid method;
                            Networks.Base.Message msg = Extension.ParseMessage(data, out method);

                            if (Commands.ContainsKey(method)) {
                                Console.WriteLine("Got command for " + Commands[method].Method.Name + " with arguments " + msg.message);
                                Commands[method].Invoke(this, msg);
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
                                        SendMessage(new Message(origin, new User(), handler.HelpText));
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

            Console.WriteLine("Sending response: " + (Responses) dataToSend[0]);
            conn.Send(dataToSend);
        }


        /// <summary>
        /// Prepares a message for transmission between client and server.
        /// </summary>
        /// <param name="destination">For extensions, the destination is unused. For 
        /// incoming messages, the destination or method is the location the message is 
        /// trying to access in the extension.</param>
        /// 
        /// <param name="message">The message contains all the information about the message- 
        /// origin point, sender, type, etc.</param>
        /// <returns></returns>
        public static byte[] PrepareMessage(Guid destination, Networks.Base.Message message) {

            // Encode sender and message
            byte[] messageAsBytes = message.ConvertToBytes();

            byte[] rawMessage = new byte[17 + messageAsBytes.Length];

            // State this is a message to reciever
            rawMessage[0] = (byte) Responses.Message;

            // Copy message origin (destination here) to array
            Array.Copy(destination.ToByteArray(), 0, rawMessage, 1, 16);

            // Copy message information in
            Array.Copy(messageAsBytes, 0, rawMessage, 17, messageAsBytes.Length);

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

            byte[] messageBytes = new byte[data.Length - 17];
            Array.Copy(data, 17, messageBytes, 0, messageBytes.Length);

            Message message = new Message(messageBytes);


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
            byte[] rawMessage = PrepareMessage(this.Identifier, message);
            conn.Send(rawMessage);
        }
    }
}

