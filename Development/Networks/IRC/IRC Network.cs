using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using Nini.Config;
using System.Diagnostics;
using Ostenvighx.Suibhne.Services.Chat;
using Newtonsoft.Json.Linq;

namespace Ostenvighx.Suibhne.Services.Irc {

    /// <summary>
    /// An irc connection manages a connection to a typical IRC server.
    /// </summary>
    public class IrcNetwork : Chat.ChatService {
        #region Network I/O
        /// <summary>
        /// The base TCP connection to the IRC Network.
        /// </summary>
        protected Socket _conn;

        /// <summary>
        /// Global buffer for incoming data on socket.
        /// </summary>
        protected byte[] GlobalBuffer;

        /// <summary>
        /// Port number being used by the socket.
        /// </summary>
        protected int port;
        #endregion

        protected String unfinishedData;

        /// <summary>
        /// Used during MODE parsing. Key is user hostmask. Value-key is user nickname, value-value is access level.
        /// </summary>
        private Dictionary<String, Dictionary<string, byte>> TempUserAccessLevels;

        /// <summary>
        /// Create a new Networks.Irc _conn object using the default Networks.Irc Configuration.
        /// The default Networks.Irc Configuration attempts to connect to a local server (on the host machine).
        /// </summary>
        public IrcNetwork() {
            this._conn = null;
            this.GlobalBuffer = new byte[2048];

            this.Status = Services.Reference.ConnectionStatus.NotReady;

            this._conn = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            this.Me = new Chat.User();
            this.Server = new Chat.Location("localhost", Services.Chat.Reference.LocationType.Network);

            this.port = 6667;

            this.TempUserAccessLevels = new Dictionary<string, Dictionary<string, byte>>();
            this.unfinishedData = "";
        }

        public IrcNetwork(Guid id) : this() {
            this.Identifier = id;
        }

        /// <summary>
        /// Create a new Networks.Irc _conn object.
        /// </summary>
        /// <param name="host">Hostname of IRC server.</param>
        /// <param name="port">Port number of the server.</param>
        /// <param name="DisplayName">Nickname to try and use on the server.</param>
        /// <param name="Username">Username.</param>
        /// <param name="realname">Real Name - A longer message to use for user details.</param>
        /// <param name="password">Password for the server connection.</param>
        /// <param name="authPass">Password to use for nickserv.</param>
        public IrcNetwork(String host, int port, String nickname, String username, String realname = "", String password = "", String authPass = "")
            : this() {
            this.Me = new Chat.User(username, authPass, nickname);
            this.Server = new Chat.Location(host, password, Chat.Reference.LocationType.Network);

            this.port = port;
        }

        /// <summary>
        /// Create a new IRC _conn object with the specified configuration values.
        /// </summary>
        /// <param name="config">Configuration to use to connect to the server.</param>
        public IrcNetwork(IConfigSource config) : this() {
            DoNetworkSetup(config);
        }

        /// <summary>
        /// Sends a NICK command to the server to try and change the bot's current DisplayName.
        /// </summary>
        /// <param name="DisplayName">Nickname to change to.</param>
        /// <param name="log">If true, sets Me LastDisplayName value to old DisplayName.</param>
        public void ChangeNickname(String nickname, Boolean log = true) {
            if (Status == Services.Reference.ConnectionStatus.Connected && nickname != null && nickname != "" && nickname != Me.DisplayName) {
                SendRaw("NICK :" + nickname);

                Chat.User tmpMe = new Chat.User(Me.UniqueID, log ? Me.DisplayName : Me.LastDisplayName, nickname);
                if (log) Me = tmpMe;
            }
        }

        public override string[] GetSupportedEvents() {
            return new string[] {
                "user_joined",
                "user_left",
                "user_quit",
                "message_received",
                "user_changed",
                "topic_changed",

                "network_connected",
                "network_disconnected"
            };
        }

        /// <summary>
        /// Part a locationID on the server with a specified message.
        /// </summary>
        /// <param name="locationID">Public to leave. Must be in the current locationID list.</param>
        /// <param name="reason">Message to send upon leaving the locationID.</param>
        public void LeaveLocation(String locationName, String reason) {
            if (Status == Services.Reference.ConnectionStatus.Connected) {
                if (locationName != null && locationName != "") {
                    Chat.Location l = new Location(locationName);

                    SendRaw("PART " + l.Name + " :" + reason);
                    base.LocationLeft(l);
                }
            }
        }

        /// <summary>
        /// Sends a well-formed message, using the message's location as the destination and other
        /// values as expected. The message's SENDER parameter does not matter, but it should
        /// be kept in practice that it matches the bot's current DisplayName on the server.
        /// </summary>
        /// <param name="message">Message to send to the server.</param>
        public override void SendMessage(String routing, Chat.Message message) {
            if (Status == Services.Reference.ConnectionStatus.Connected && message != null) {

                try {
                    JObject r = JObject.Parse(routing);
                    String location = "";
                    if (r["respond_to"] == null)
                        throw new FormatException("Cannot route message, target not defined.");

                    location = r["respond_to"].ToString();

                    SendRaw("PRIVMSG " + location + " :" +
                    // (message.IsAction ? "\u0001ACTION " : "") + 
                    message.message
                    // + (m.IsAction ? "\u0001" : "")
                  );
                }

                catch (Exception e) { }


            }
        }

        /// <summary>
        /// Send a raw command to the server.
        /// </summary>
        /// <param name="message">Line to send to the server.</param>
        /// <param name="log">If set to <c>true</c>, log the raw command to the output window. (Enabled by default)</param>
        public void SendRaw(String data) {
            if (this._conn.Connected && Status != Services.Reference.ConnectionStatus.Disconnected) {
                byte[] bdata = Encoding.UTF8.GetBytes(data + "\r\n");
                _conn.Send(bdata);
            }
        }

        public override void Setup(String configBase) {

            DirectoryInfo di = new DirectoryInfo(configBase);

            // Get config root by going up a couple of levels (ROOT/Networks/Identifier/)
            this.ConfigRoot = di.FullName;

            IniConfigSource configLoaded = new IniConfigSource(ConfigRoot + "/Services/" + Identifier + "/service.ini");
            configLoaded.CaseSensitive = false;

            DoNetworkSetup(configLoaded);
        }

        /// <summary>
        /// Perform a connection to the server.
        /// </summary>
        public override void Start() {
            if (Status == Services.Reference.ConnectionStatus.Disconnected) {
                Status = Services.Reference.ConnectionStatus.Connecting;
                try {

                    _conn.BeginConnect(Server.Name, 6667, new AsyncCallback(ConnectionCompleteCallback), _conn);
                }

                catch (Exception e) {
                    Console.WriteLine(e);
                }
            }
        }

        /// <summary>
        /// Closes the connection to the server after finishing the input buffer.
        /// Defaults with quit message "Leaving".
        /// </summary>
        /// <param name="reason">Specifies a quit message to send in place of default.</param>
        public override void Stop(String reason = "Leaving") {
            SendRaw("QUIT :" + reason);
            Status = Services.Reference.ConnectionStatus.Disconnected;
            base.DisconnectionComplete();
        }

        /// <summary>
        /// Callback handler for when a connection is completed to the server.
        /// </summary>
        /// <param name="ar">Result.</param>
        internal void ConnectionCompleteCallback(IAsyncResult ar) {
            try {
                Socket client = (Socket)ar.AsyncState;
                client.EndConnect(ar);

                client.BeginReceive(GlobalBuffer, 0, GlobalBuffer.Length, SocketFlags.None, DataRecievedCallback, client);

                SendRaw(String.Format("USER {0} 8 * :{0}", Me.UniqueID));

                Thread.Sleep(100);

                // Set initial DisplayName
                SendRaw("NICK " + Me.DisplayName);
            }

            catch (SocketException se) {
                Console.WriteLine("Error connecting to " + Identifier + ": " + se.Message);
                this.Stop();
            }

        }

        /// <summary>
        /// Data recieved callback.
        /// </summary>
        /// <param name="ar"></param>
        internal void DataRecievedCallback(IAsyncResult ar) {

            Socket recievedOn = (Socket)ar.AsyncState;
            try {
                int recievedAmount = recievedOn.EndReceive(ar);

                byte[] btemp = new byte[recievedAmount];
                Array.Copy(GlobalBuffer, btemp, recievedAmount);

                String text = Encoding.UTF8.GetString(btemp);

                HandleDataPacket(text);

                recievedOn.BeginReceive(GlobalBuffer, 0, GlobalBuffer.Length, SocketFlags.None, DataRecievedCallback, recievedOn);

            }

            catch (SocketException se) {
                // Probably disconnected
                Console.WriteLine(se);
            }

            catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        internal void HandleDataPacket(String data) {

            if (unfinishedData != "") {
                String newData = data.Split('\n')[0];
                data = data.Remove(0, newData.Length + 1);
                unfinishedData += newData;
                this.HandleData(unfinishedData);
                unfinishedData = "";
            }

            String[] lines = data.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (String line in lines) {

                String lineTrimmed = line.TrimStart(new char[] { ':' }).TrimEnd('\r');
                if (line.EndsWith("\r")) {

                    if (lineTrimmed.ToLower().StartsWith("ping"))
                        HandlePing(lineTrimmed);
                    else
                        HandleData(lineTrimmed);
                } else {
                    // Unfinished line
                    Console.WriteLine("Unfinished line: " + line);
                    unfinishedData = lineTrimmed;
                }

            }
        }

        /// <summary>
        /// Join a locationID on the server. This will automatically check the location
        /// password as well, if it is defined it will use it.
        /// </summary>
        /// <param name="locationID">Public (as an Location) to join.</param>
        internal void JoinLocation(String name) {
            if (Status == Services.Reference.ConnectionStatus.Connected) {

                // Load location information
                IniConfigSource l = new IniConfigSource(ConfigRoot + "/Services/" + this.Identifier + "/Locations/" + name + ".ini");
                l.CaseSensitive = false;

                Chat.Location location = new Chat.Location("");
                if (l.Configs["Location"] != null && l.Configs["Location"].GetString("Name") != null) {
                    location.Name = l.Configs["Location"].GetString("name");
                } else {
                    return;
                }

                location.Password = l.Configs["Location"].GetString("Password", "");
                if (location.Password != "") {
                    SendRaw("JOIN " + location.Name + " " + location.Password);
                } else {
                    SendRaw("JOIN " + location.Name);
                }

                location.Parent = this.Identifier;
                SendRaw("WHO " + location.Name);

                base.LocationJoined(location);
            }
        }

        internal void SendMessage(Message m) {
            JObject routing = new JObject();
            routing.Add("serviceID", Identifier);
            routing.Add("respond_to", m.location);

            SendMessage(routing.ToString(), m);
        }

        /// <summary>
        /// Internal method to handle a NICK line sent from the server.
        /// </summary>
        /// <param name="line">Line to parse.</param>
        protected void HandleNicknameChange(String line) {
            String[] dataChunks = line.Split(new char[] { ' ' });

            User changer = User.Parse(dataChunks[0]);
            changer.LastDisplayName = changer.DisplayName;
            changer.DisplayName = dataChunks[2].TrimStart(':');

            if (changer.LastDisplayName == Me.DisplayName) {
                Me.LastDisplayName = Me.DisplayName;
                Me.DisplayName = changer.DisplayName;
            }
        }

        // Hook connection complete, make sure we identify before triggering rest of the base handler actions.
        protected override void ConnectionComplete() {
            Debug.WriteLine("Connection complete on " + Identifier);

            if (Me.LastDisplayName != "") {
                Message message = new Message(null, "IDENTIFY " + Me.LastDisplayName);
                message.IsPrivate = true;
                message.target = new Chat.User("NickServ");

                SendMessage(message);
            }

            this.ConnectToChannels();

            base.ConnectionComplete();
        }

        /// <summary>
        /// Method to handle incoming data, line by line.
        /// Actual parsing is done here, not in DataRecievedCallback.
        /// </summary>
        /// <param name="line">Line of data being handled.</param>
        protected virtual void HandleData(String line) {
            String[] dataChunks = line.Split(new char[] { ' ' });
            try {
                switch (dataChunks[1].ToLower()) {

                    #region Numeric Codes
                    case "001":
                        // Network welcome message
                        // Network.locationName = dataChunks[0].TrimStart(new char[] { ':' });
                        Status = Services.Reference.ConnectionStatus.Authenticating;
                        break;

                    case "315":
                        // End of WHO response
                        // ParseWhoList(line);
                        break;


                    case "352":
                        // who response
                        // ParseWhoResponse(line);
                        break;

                    case "353":
                    case "366":
                        #region Names response
                        String chan = "#channel";
                        switch (dataChunks[1].ToLower()) {

                            case "353":
                                chan = line.Split(new char[] { ' ' })[4].Trim().ToLower();
                                // "irc.ostenvighx.co 353 Suibhne = #suibhne :Suibhne Ted @Delenas"
                                String[] namesList = line.Split(new String[] { " :" }, StringSplitOptions.None)[1].Trim().Split(new char[] { ' ' });

                                // Do something with names here maybe
                                break;

                            case "366":
                                chan = line.Split(new char[] { ' ' })[3].Trim().ToLower();
                                break;
                        }

                        #endregion
                        break;

                    case "372":
                        // Message of the day
                        break;

                    case "376":
                        // End MOTD
                        Status = Services.Reference.ConnectionStatus.Connected;
                        ConnectionComplete();
                        break;

                    case "422":
                        Status = Services.Reference.ConnectionStatus.Connected;
                        ConnectionComplete();
                        break;

                    case "433":
                        // Nickname in use - do not log because going to identify with LastDisplayName value
                        ChangeNickname(Me.DisplayName + "-", false);
                        break;

                    #endregion

                    #region User Events
                    case "nick":
                        HandleNicknameChange(line);
                        break;

                    case "join":
                        User joiner = User.Parse(dataChunks[0]);
                        this.HandleUserJoined(dataChunks[2].TrimStart(':'), joiner);
                        break;

                    case "part":
                        User parter = User.Parse(dataChunks[0]);
                        this.HandleUserLeft(dataChunks[2].TrimStart(':'), parter);
                        break;

                    case "quit":
                        Chat.User quitter = User.Parse(dataChunks[0]);
                        this.HandleUserQuit(quitter);
                        break;
                    #endregion

                    case "topic":
                        Chat.User u = User.Parse(dataChunks[0]);
                        String topic = line.Substring(line.IndexOf(" :") + 2).TrimEnd(new char[] { '\r', '\n' }).Trim();

                        Console.WriteLine(u.DisplayName + " has changed the topic to " + topic);

                        // TODO: Reimplement topic changed
                        // String json = "{ \"event\": \"topic_changed\", \"location\": \"" + id.ToString() + "\", \"changer\": { \"unique_id\": \"" + u.UniqueID + "\", \"display_name\": \"" + u.DisplayName + "\"}, \"topic\": \"" + topic + "\"}";
                        // FireEvent(json);
                        break;

                    case "mode":
                        HandleModeChange(line);
                        break;


                    case "privmsg":
                    case "notice":
                        if (Status == Services.Reference.ConnectionStatus.Connected)
                            HandleIncomingMessage(line);
                        break;

                    default:
                        // Console.WriteLine("Data recieved: " + line);
                        break;
                }
            }

            catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        protected void HandleIncomingMessage(String line) {
            Message msg = Message.Parse(this, line);

            String hostmask = "";
            string userhost = line.Split(new char[] { ' ' })[0];
            Match hostmaskMatch = RegularExpressions.USER_REGEX.Match(userhost);
            if (hostmaskMatch.Success) hostmask = hostmaskMatch.Groups["hostname"].Value;

            JObject extra = new JObject();

            // Add routing information
            JObject routing = new JObject();
            routing.Add("respond_to", msg.location);
            extra.Add("routing", routing);

            // Add message action information
            JObject message = new JObject();
            message.Add("is_action", msg.IsAction);
            extra.Add("message", message);

            base.MessageRecieved(msg, extra.ToString());
        }
        
        /// <summary>
        /// Deal with incoming PING requests and send matching PONG commands back to
        /// the server that sent them.
        /// </summary>
        /// <param name="line">Line. Required to strip PONG data from.</param>
        protected void HandlePing(String line) {
            String[] dataChunks = line.Split(new char[] { ' ' });
            SendRaw("PONG " + dataChunks[1]);
        }

        protected void HandleUserLeft(String locationName, Chat.User u) {
            // Make sure we only send a new names list if it's not US that left.
            if (u.DisplayName.ToLower() != Me.DisplayName.ToLower())
                SendRaw("WHO " + locationName);

            base.UserLeft(new Location(locationName), u);
        }

        private void ConnectToChannels() {
            foreach (String filename in Directory.GetFiles(ConfigRoot + "Services/" + Identifier + "/Locations/", "*.ini", SearchOption.TopDirectoryOnly)) {
                String fn = new FileInfo(filename).Name;
                JoinLocation(fn.Substring(0, fn.IndexOf('.')));
            }
        }

        private void DoNetworkSetup(IConfigSource config) {
            this.Me = new Chat.User(
                config.Configs["Account Settings"].GetString("userName", "user"),
                config.Configs["Authentification"].GetString("authPass", ""),
                config.Configs["Account Settings"].GetString("displayName", "IrcUser"));

            this.Server = new Chat.Location(
                config.Configs["Host Settings"].GetString("host", "localhost"),
                config.Configs["Host Settings"].GetString("servPass", ""),
                Chat.Reference.LocationType.Network);

            this.port = config.Configs["Host Settings"].GetInt("port", 6667);
            this.Status = Services.Reference.ConnectionStatus.Disconnected;
        }
        private void HandleModeChange(String line) {
            Match match = Regex.Match(line, RegularExpressions.SENDER_REGEX_RAW + @" MODE " + RegularExpressions.LOCATION_REGEX + @" " + @"(?<data>.*)", RegexOptions.ExplicitCapture);
            if (!match.Success)
                return;

            SendRaw("WHO " + match.Groups["location"].Value);
        }

        private void HandleUserJoined(String locationName, User u) {
            base.UserJoined(new Location(locationName), u);
            SendRaw("WHO " + locationName);
        }

        private void HandleUserQuit(Chat.User quitter) {
            base.UserQuit(this.Server, quitter);
        }
        
        // TODO: Reimplement access levels at a later date. System needs work.
        /*
        #region Who Responses
        /// <summary>
        /// Goes through the temporary who feedback and gets all the hostmasks, translating them into ban strings for the final access list
        /// </summary>
        private void ParseWhoList(String line) {
            String[] lineBits = line.Split(new char[] { ' ' });

            Chat.Location location = new Location(lineBits[3]);
            location.AccessLevels.Clear();

            foreach (KeyValuePair<string, Dictionary<string, byte>> hostmaskItem in TempUserAccessLevels) {
                if (hostmaskItem.Value.Count > 1) {
                    // Remap keys
                    foreach (KeyValuePair<string, byte> user in hostmaskItem.Value) {
                        location.AccessLevels.Add(user.Key + "@" + hostmaskItem.Key, user.Value);
                    }
                } else {
                    location.AccessLevels.Add("*@" + hostmaskItem.Key, hostmaskItem.Value.Values.ElementAt(0));
                }
            }

            TempUserAccessLevels.Clear();
        }

        private void ParseWhoResponse(String line) {

            /// :foxtaur.furnet.org 352 Delenas #ostenvighx ~Delenas fur-3EB9DC59.hsd1.pa.comcast.net foxtaur.furnet.org Delenas Hr~ :0 Delenas Freshtt
            /// 
            String[] bits = line.Split(new char[] { ' ' });
            Guid locationGuid = GetLocationIdByName(bits[3]);
            User u = new User();
            u.UniqueID = bits[4].TrimStart(new char[] { '~' });
            u.DisplayName = bits[7];

            String userHost = bits[5];
            String modesRaw = bits[8];

            byte level = User.GetAccessLevel(modesRaw);
            if (level == 0) level = 1;

            // Get the location we're working on- if we joined the channel, we have this, but we best make sure
            Guid location = GetLocationIdByName(bits[3]);
            if (location == Guid.Empty)
                return;

            // Make sure we have a valid list for the hostmask
            if (!TempUserAccessLevels.ContainsKey(userHost))
                TempUserAccessLevels.Add(userHost, new Dictionary<string, byte>());

            TempUserAccessLevels[bits[5]].Add(bits[7], level);

        }
        #endregion
        */
    }
}

