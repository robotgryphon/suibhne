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
        /// Port number being used by the socket.
        /// </summary>
        protected int port;

        /// <summary>
        /// Global buffer for incoming data on socket.
        /// </summary>
        protected byte[] GlobalBuffer;
        #endregion

        protected String unfinishedData;

        #region Data Events
        /// <summary>
        /// Fired when any incomind data is recieved. This is the absolute lowest-level
        /// event and should really only be hooked into if there is not a more suitable
        /// event to hook into.
        /// </summary>
        public event Reference.IrcDataEvent OnDataRecieved;

        #endregion

        #region Location Events
        /// <summary>
        /// Occurs when locations begin to be listened at. Examples include started queries and
        /// joining IRC Channels.
        /// </summary>
        public event Reference.IrcLocationEvent OnListeningStart;

        /// <summary>
        /// Occurs when locations stop being listened at. A good example would be a parted locationID.
        /// </summary>
        public event Reference.IrcLocationEvent OnListeningEnd;
        #endregion

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

            this.Listened = new Dictionary<Guid, Chat.Location>();
            this.Status = Services.Reference.ConnectionStatus.NotReady;

            this._conn = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            this.Me = new Chat.User();
            this.Server = new Chat.Location("localhost", Services.Chat.Reference.LocationType.Network);

            this.port = 6667;

            this.TempUserAccessLevels = new Dictionary<string, Dictionary<string, byte>>();
            this.unfinishedData = "";
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
        public IrcNetwork(IConfigSource config)
            : this() {
            DoNetworkSetup(config);
        }

        public override void Setup(String configFile) {

            DirectoryInfo di = new DirectoryInfo(configFile);

            // Get config root by going up a couple of levels (ROOT/Networks/Identifier/)
            this.ConfigRoot = di.Parent.Parent.Parent.FullName;

            IniConfigSource configLoaded = new IniConfigSource(configFile);
            configLoaded.CaseSensitive = false;

            DoNetworkSetup(configLoaded);
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

        /// <summary>
        /// Perform a connection to the server.
        /// </summary>
        public override void Connect() {
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

            catch (SocketException) {
                this.Disconnect();
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

        // Hook connection complete, make sure we identify before triggering rest of the base handler actions.
        protected override void HandleConnectionComplete() {
            if (Me.LastDisplayName != "") {
                Message message = new Message(this.Identifier, null, "IDENTIFY " + Me.LastDisplayName);
                message.IsPrivate = true;
                message.target = new Chat.User("NickServ");

                SendMessage(message);
            }

            base.HandleConnectionComplete();
        }
        /// <summary>
        /// Method to handle incoming data, line by line.
        /// Actual parsing is done here, not in DataRecievedCallback.
        /// </summary>
        /// <param name="line">Line of data being handled.</param>
        protected virtual void HandleData(String line) {
            if (this.OnDataRecieved != null) {
                OnDataRecieved(this, line);
            }

            String[] dataChunks = line.Split(new char[] { ' ' });
            try {
                switch (dataChunks[1].ToLower()) {

                    #region Numeric Codes
                    case "001":
                        // Network welcome message
                        // Network.locationName = dataChunks[0].TrimStart(new char[] { ':' });
                        break;

                    case "315":
                        // End of WHO response
                        ParseWhoList(line);
                        break;


                    case "352":
                        // who response
                        ParseWhoResponse(line);
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
                        HandleConnectionComplete();
                        break;

                    case "422":
                        Status = Services.Reference.ConnectionStatus.Connected;
                        HandleConnectionComplete();
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
                        Chat.User joiner = User.Parse(dataChunks[0]);
                        HandleUserJoin(GetLocationIdByName(dataChunks[2].TrimStart(':')), joiner);
                        break;

                    case "part":
                        Chat.User parter = User.Parse(dataChunks[0]);
                        HandleUserLeave(GetLocationIdByName(dataChunks[2].TrimStart(':')), parter);
                        break;

                    case "quit":
                        Chat.User quitter = User.Parse(dataChunks[0]);
                        HandleUserQuit(Identifier, quitter);

                        foreach (Chat.Location listened in Listened.Values) {
                            string hostmask = dataChunks[0].Substring(line.IndexOf("@") + 1);
                            if (listened.AccessLevels.ContainsKey("*@" + hostmask))
                                listened.AccessLevels.Remove("*@" + hostmask);

                            if (listened.AccessLevels.ContainsKey(quitter.DisplayName + "@" + hostmask))
                                listened.AccessLevels.Remove(quitter.DisplayName + "@" + hostmask);
                        }
                        break;
                    #endregion

                    case "topic":
                        Chat.User u = User.Parse(dataChunks[0]);
                        String topic = line.Substring(line.IndexOf(" :") + 2).TrimEnd(new char[] { '\r', '\n' }).Trim();

                        Console.WriteLine(u.DisplayName + " has changed the topic to " + topic);
                        Guid id = this.GetLocationIdByName(dataChunks[2]);
                        String json = "{ \"event\": \"topic_changed\", \"location\": \"" + id.ToString() + "\", \"changer\": { \"unique_id\": \"" + u.UniqueID + "\", \"display_name\": \"" + u.DisplayName + "\"}, \"topic\": \"" + topic + "\"}";

                        FireEvent(json);

                        break;

                    case "mode":
                        HandleModeChange(line);
                        break;


                    case "privmsg":
                    case "notice":
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

            string extraJSON = "{" +
                "\"message\": " + "{" +
                    "\"is_action\": " + (msg.IsAction ? "true":"false") + "," +
                    "\"is_private\": " + (msg.IsPrivate ? "true" : "false") +
               "}" +
            "}";

            base.HandleMessageRecieved(msg, extraJSON);
        }

        /// <summary>
        /// Join a locationID on the server. This will automatically check the location
        /// password as well, if it is defined it will use it.
        /// </summary>
        /// <param name="locationID">Public (as an Location) to join.</param>
        public override void JoinLocation(Guid locationID) {
            if (Status == Services.Reference.ConnectionStatus.Connected) {
                if (locationID != Guid.Empty && !this.Listened.ContainsKey(locationID)) {

                    // Load location information
                    IniConfigSource l = new IniConfigSource(ConfigRoot + "/Services/" + this.Identifier + "/Locations/" + locationID + "/location.ini");
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
                    Listened.Add(locationID, location);

                    SendRaw("WHO " + location.Name);

                    if (this.OnListeningStart != null) {
                        OnListeningStart(this, locationID);
                    }
                }
            }
        }

        public override void LeaveLocation(Guid g) {
            LeaveLocation(g, "Leaving");
        }

        /// <summary>
        /// Part a locationID on the server with a specified message.
        /// </summary>
        /// <param name="locationID">Public to leave. Must be in the current locationID list.</param>
        /// <param name="reason">Message to send upon leaving the locationID.</param>
        public void LeaveLocation(Guid locationID, String reason) {
            if (Status == Services.Reference.ConnectionStatus.Connected) {
                if (locationID != null && locationID != Guid.Empty) {
                    if (Listened.ContainsKey(locationID)) {
                        Chat.Location l = Listened[locationID];
                        if (this.OnListeningEnd != null) {
                            OnListeningEnd(this, locationID);
                        }

                        SendRaw("PART " + Listened[locationID].Name + " :" + reason);
                        this.Listened.Remove(locationID);
                    } else {
                        throw new Exception("Location not found or already exited.");
                    }
                }
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

        // TODO: Implement event handling so we can handle actions again
        private void SendMessage(Message m) {
            if (Status == Services.Reference.ConnectionStatus.Connected && m != null) {

                if (Listened.ContainsKey(m.locationID) || m.locationID == Identifier) {
                    string location;
                    if (m.locationID == this.Identifier && m.IsPrivate)
                        location = m.target.DisplayName;
                    else
                        location = Listened[m.locationID].Name;

                    SendRaw("PRIVMSG " + location + " :" + (m.IsAction ? "\u0001ACTION " : "") + m.message + (m.IsAction ? "\u0001" : ""));
                } else {
                    // Have not joined location
                    Console.WriteLine("Location not available: " + m.locationID);
                }
            }
        }

        /// <summary>
        /// Sends a well-formed message, using the message's location as the destination and other
        /// values as expected. The message's SENDER parameter does not matter, but it should
        /// be kept in practice that it matches the bot's current DisplayName on the server.
        /// </summary>
        /// <param name="message">Message to send to the server.</param>
        public override void SendMessage(Chat.Message message) {
            SendMessage(message);
        }

        protected override void HandleUserJoin(Guid l, Chat.User u) {
            base.HandleUserJoin(l, u);
            SendRaw("WHO " + Listened[l].Name);
        }

        protected override void HandleUserLeave(Guid l, Chat.User u) {
            // Make sure we only send a new names list if it's not US that left.
            if (u.DisplayName.ToLower() != Me.DisplayName.ToLower())
                SendRaw("WHO " + Listened[l].Name);

            base.HandleUserLeave(l, u);
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

            foreach (Chat.Location location in Listened.Values) {
                foreach (String host in location.AccessLevels.Keys) {
                    if (host.StartsWith(changer.LastDisplayName)) {
                        byte accessLevel = location.AccessLevels[host];
                        location.AccessLevels.Remove(host);
                        location.AccessLevels.Add(changer.DisplayName + "@" + host.Substring(host.IndexOf("@") + 1), accessLevel);

                        break;
                    }
                }
            }
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

        /// <summary>
        /// Closes the connection to the server after finishing the input buffer.
        /// Defaults with quit message "Leaving".
        /// </summary>
        /// <param name="reason">Specifies a quit message to send in place of default.</param>
        public override void Disconnect(String reason = "Leaving") {
            SendRaw("QUIT :" + reason);
            Status = Services.Reference.ConnectionStatus.Disconnected;
            base.HandleDisconnectComplete();
        }


        private void HandleModeChange(String line) {
            Match match = Regex.Match(line, RegularExpressions.SENDER_REGEX_RAW + @" MODE " + RegularExpressions.LOCATION_REGEX + @" " + @"(?<data>.*)", RegexOptions.ExplicitCapture);
            if (!match.Success)
                return;

            SendRaw("WHO " + match.Groups["location"].Value);
        }

        #region Who Responses
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

        /// <summary>
        /// Goes through the temporary who feedback and gets all the hostmasks, translating them into ban strings for the final access list
        /// </summary>
        private void ParseWhoList(String line) {
            String[] lineBits = line.Split(new char[] { ' ' });
            Guid locationID = GetLocationIdByName(lineBits[3]);
            Chat.Location location = Listened[locationID];
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
        #endregion

        public override string[] GetSupportedEvents() {
            return new string[] {
                "user_joined",
                "user_left",
                "user_quit",
                "message_recieved",
                "user_changed",
                "topic_changed",

                "network_connected",
                "network_disconnected"
            };
        }
    }
}

