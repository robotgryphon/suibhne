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
using System.Runtime.InteropServices;

namespace Ostenvighx.Suibhne.Extensions {

    /// <summary>
    /// An extension suite holds a filename to an extension executable, the IDs of the extensions in
    /// the executable suite, and any information about the suite. (Author, Version, etc.)
    /// </summary>
    public abstract class Extension {

        public Guid Identifier {
            get {
                return Guid.Parse(
                    ((GuidAttribute) GetType().Assembly.GetCustomAttribute(typeof(GuidAttribute))).Value
                  );
            }

            private set { }
        }

        protected Socket conn;
        protected byte[] buffer;
        public Boolean Connected {
            get;
            protected set;
        }


        public delegate void CommandHandler(Extension e, String json);
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
            Console.WriteLine("Starting extension as " + Identifier);
            MapCommandMethods();
            Connect();
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
                            commandMethodParams[0].ParameterType != typeof(Extension) || commandMethodParams[1].ParameterType != typeof(String))
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
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine("Failed to start extension.");
                Console.WriteLine("Reason: " + e.Message);

                if (this.OnServerDisconnect != null)
                    OnServerDisconnect(this);

                this.Connected = false;
            }

        }

        protected void ConnectedCallback(IAsyncResult result) {
            conn = (Socket)result.AsyncState;
            try {
                conn.EndConnect(result);
                Connected = true;
                conn.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, RecieveDataCallback, conn);

                Activate();
            }

            catch (SocketException se) {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine("Failed to start extension- the connection failed.");
                Console.WriteLine("Reason: " + se.Message);

                if (this.OnExtensionExit != null)
                    OnExtensionExit(this);

                this.Connected = false;
            }
        }

        protected virtual void Activate() {
            JObject activation = new JObject();
            activation.Add("event", "extension_activation");
            activation.Add("extid", Identifier);
            activation.Add("name", Name);
            activation.Add("required_events", new JArray());
            activation.Add("optional_events", new JArray());

            conn.Send(Encoding.UTF32.GetBytes(activation.ToString()));
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
                    case "extension_details":
                        string response =
                            "[" + Identifier + "] " + Name + " (v. " + Version + ")" + " developed by " + Author;

                        SendMessage(new Message(Guid.Parse(Event["location"]["id"].ToString()), new User(), response));
                        break;

                    case "command_recieved":
                        if (Event["handler"] != null && Event["handler"].ToString().Trim() != "") {
                            if (CommandHandlers.ContainsKey(Event["handler"].ToString())) {
                                CommandHandlers[Event["handler"].ToString()].Invoke(this, Event.ToString());
                            }
                        }
                        break;

                    case "command_help":

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

                    case "message_recieved":
                        HandleIncomingMessage(Event.ToString());
                        break;


                    case "extension_shutdown":
                        conn.Shutdown(SocketShutdown.Both);
                        conn.Close();
                        Connected = false;

                        if (this.OnExtensionExit != null) {
                            OnExtensionExit(this);
                        }
                        break;

                    case "user_joined":
                    case "user_left":
                    case "user_name_changed":
                    case "user_quit":
                        HandleUserEvent(Event.ToString());
                        break;
                }

            }

            catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        protected virtual void HandleUserEvent(string json) { }

        protected virtual void HandleIncomingMessage(string json) { }

        public void SendMessage(Networks.Base.Message message) {
            JObject msg = new JObject();
            msg.Add("event", "message_send");
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

