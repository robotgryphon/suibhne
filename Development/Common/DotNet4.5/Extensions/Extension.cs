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


        public delegate void CommandHandler(Extension e, JObject json);
        public delegate void ExtensionEvent(Extension e);

        protected Dictionary<String, CommandHandler> CommandHandlers;

        public event ExtensionEvent OnServerDisconnect;
        public event ExtensionEvent OnExtensionExit;

        public String Name {
            get { return ((AssemblyTitleAttribute)this.GetType().Assembly.GetCustomAttributes(typeof(AssemblyTitleAttribute), true)[0]).Title.ToString(); }
            protected set { }
        }

        public String Author {
            get { return ((AssemblyCompanyAttribute)this.GetType().Assembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), true)[0]).Company.ToString(); }
            protected set { }
        }

        public String Version {
            get { return this.GetType().Assembly.GetName().Version.ToString(); }
            protected set { }
        }

        public Extension() {
            this.buffer = new byte[4096];
            this.conn = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            conn.SendBufferSize = 4096;
            conn.ReceiveBufferSize = 4096;

            this.Connected = false;

            this.CommandHandlers = new Dictionary<String, CommandHandler>();
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

                this.Identifier = new Guid((String)config.GetValue("Identifier"));

            } else {

                // Terminate, extension not installed properly
                throw new FileNotFoundException("System information file not found. Please re-install the extension.");
            }
        }

        private void MapCommandMethods() {
            // Get all possible command handler methods and create a mapping dictionary
            MethodInfo[] definedMethods = this.GetType().GetMethods();
            foreach (MethodInfo method in definedMethods) {
                Object[] attrs = method.GetCustomAttributes(typeof(CommandHandlerAttribute), false);
                foreach (Object attr in attrs) {
                    if (attr.GetType() == typeof(CommandHandlerAttribute)) {
                        CommandHandlerAttribute handler = (CommandHandlerAttribute)attr;
                        Console.WriteLine("Got command handler: " + handler.Name + " (maps to " + method.Name + ")");

                        ParameterInfo[] commandMethodParams = method.GetParameters();

                        if (commandMethodParams.Length != 2 || (
                            commandMethodParams[0].ParameterType != typeof(Extension) || commandMethodParams[1].ParameterType != typeof(JObject))
                        )
                            throw new Exception("Invalid method signature for command handler: " + method.DeclaringType.FullName + ":" + method.Name);

                        CommandHandler methodDelegate = (CommandHandler)Delegate.CreateDelegate(typeof(CommandHandler), null, method);
                        this.CommandHandlers.Add(handler.Name, methodDelegate);
                    }
                }
            }
        }

        public virtual void Connect() {

            try {
                Console.WriteLine("Starting conn");
                conn.BeginConnect("127.0.0.1", 6700, ConnectedCallback, conn);
            }

            catch (Exception e) {
                Console.WriteLine("Failed to start extension.");
                Console.WriteLine(e);
                if (this.OnServerDisconnect != null)
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

                conn.Send(Encoding.UTF32.GetBytes("{ \"event\": \"extension.activate\", \"extid\": \"" + Identifier + "\" }"));

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

                string json = Encoding.UTF32.GetString(data);
                JObject Event;

                try {
                    Event = JObject.Parse(json);
                }

                catch (Exception e) {
                    Console.WriteLine("Invalid json: " + json);
                    return;
                }

                if (Event == null)
                    return;

                switch (Event["event"].ToString().ToLower()) {
                    case "extension.details":
                        string response =
                            "[" + Identifier + "] " + Name + " (v. " + Version + ")" + " developed by " + Author;

                        SendMessage(new Message(Guid.Parse(Event["location"]["id"].ToString()), new User(), response));
                        break;

                    case "command.recieve":
                        if (Event["handler"] != null && Event["handler"].ToString().Trim() != "") {
                            if (CommandHandlers.ContainsKey(Event["handler"].ToString())) {
                                CommandHandlers[Event["handler"].ToString()].Invoke(this, Event);
                            }
                        }
                        break;

                    case "command.help":

                        if (CommandHandlers.ContainsKey(Event["handler"].ToString())) {
                            MethodInfo method = CommandHandlers[Event["handler"].ToString()].Method;

                            Object[] attrs = method.GetCustomAttributes(typeof(HelpAttribute), false);
                            foreach (Object attr in attrs) {
                                if (attr.GetType() == typeof(HelpAttribute)) {
                                    HelpAttribute handler = (HelpAttribute)attr;
                                    SendMessage(new Message(Guid.Parse(Event["location"]["id"].ToString()), new User(), handler.HelpText));
                                }
                            }
                        }

                        break;

                    case "message.recieve":
                        HandleIncomingMessage(Event);
                        break;


                    case "extension.shutdown":
                        conn.Shutdown(SocketShutdown.Both);
                        conn.Close();
                        Connected = false;

                        if (this.OnExtensionExit != null) {
                            OnExtensionExit(this);
                        }
                        break;

                    case "user.join":
                    case "user.leave":
                    case "user.namechange":
                    case "user.quit":
                        HandleUserEvent(Event);
                        break;
                }

            }

            catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        protected virtual void HandleUserEvent(JObject json) {
            Console.WriteLine("Got user event for user: " + json["sender"]["DisplayName"] + " of type " + json["event"]);
        }

        protected virtual void HandleIncomingMessage(JObject e) { }

        public void SendMessage(Networks.Base.Message message) {
            JObject msg = new JObject();
            msg.Add("event", "message.send");
            msg.Add("extid", this.Identifier);
            msg.Add("contents", message.message);

            JObject location = new JObject();
            location.Add("id", message.locationID);
            location.Add("type", (byte) message.type);

            if (Message.IsPrivateMessage(message)) {
                location.Add("target", message.target.DisplayName);
            }
            
            msg.Add("location", location);

            conn.Send(Encoding.UTF32.GetBytes(msg.ToString()));
        }
    }
}

