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

namespace Ostenvighx.Suibhne.Networks.Irc {

    /// <summary>
    /// An irc connection manages a connection to a typical IRC server.
    /// </summary>
    public class IrcNetwork : Base.Network {
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

        /// <summary>
        /// Used to group all users together in one "location".
        /// If the location is equal to this, then it's a private message. 
        /// Check the sender in that case.
        /// </summary>
        public Guid UserIdentifier { get; protected set; }

        protected Guid NetworkIdentifier {
            get { return GetLocationIdByName("<network>"); }
            private set { }
        }

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

        #region User Events
        /// <summary>
        /// Called when a user joins a locationID the connection is listening on.
        /// </summary>
        public event Reference.IrcUserEvent OnUserJoin;

        /// <summary>
        /// Called when a user parts a locationID the connection is listening on.
        /// </summary>
        public event Reference.IrcUserEvent OnUserPart;

        /// <summary>
        /// Called when a user quits the server the connection is at.
        /// </summary>
        public event Reference.IrcUserEvent OnUserQuit;

        /// <summary>
        /// Called when a user changes their DisplayName on the server.
        /// </summary>
        public event Reference.IrcUserEvent OnUserNickChange;

        /// <summary>
        /// Called when the connection's DisplayName changes, through a 433 code or manually.
        /// </summary>
        public event Reference.IrcUserEvent OnBotNickChange;

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

            this.Listened = new Dictionary<Guid, Base.Location>();
            this.Status = Base.Reference.ConnectionStatus.Disconnected;

            this._conn = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            this.UserIdentifier = Guid.NewGuid();
            this.Listened.Add(UserIdentifier, new Base.Location("<user>"));

            this.Me = new Base.User();
            this.Server = new Base.Location("localhost", Networks.Base.Reference.LocationType.Network);

            this.port = 6667;

            this.TempUserAccessLevels = new Dictionary<string, Dictionary<string, byte>>();
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
            this.Me = new Base.User(username, authPass, nickname);
            this.Server = new Base.Location(host, password, Base.Reference.LocationType.Network);

            this.port = port;
        }

        /// <summary>
        /// Create a new IRC _conn object with the specified configuration values.
        /// </summary>
        /// <param name="config">Configuration to use to connect to the server.</param>
        public IrcNetwork(IConfig config)
            : this() {
                DoNetworkSetup(config);
        }

        public override void Setup(String configFile) {
            IniConfigSource configLoaded = new IniConfigSource(configFile);
            configLoaded.CaseSensitive = false;
            IniConfig config = (IniConfig)configLoaded.Configs["Server"];

            DoNetworkSetup(config);            
        }

        private void DoNetworkSetup(IConfig config) {
            this.Me = new Base.User(
                config.GetString("username", "user"),
                config.GetString("authpassword", ""),
                config.GetString("nickname", "IrcUser"));

            this.Server = new Base.Location(
                config.GetString("hostname", "localhost"),
                config.GetString("password", ""),
                Base.Reference.LocationType.Network);

            this.port = config.GetInt("port", 6667);
        }

        /// <summary>
        /// Sends a NICK command to the server to try and change the bot's current DisplayName.
        /// </summary>
        /// <param name="DisplayName">Nickname to change to.</param>
        /// <param name="log">If true, sets Me LastDisplayName value to old DisplayName.</param>
        public void ChangeNickname(String nickname, Boolean log = true) {
            if (Status == Base.Reference.ConnectionStatus.Connected && nickname != null && nickname != "" && nickname != Me.DisplayName) {
                SendRaw("NICK :" + nickname);

                Base.User tmpMe = new Base.User(Me.Username, log ? Me.DisplayName : Me.LastDisplayName, nickname);
                if (log) Me = tmpMe;

                //if (this.OnBotNickChange != null) {
                //    OnBotNickChange(this, GetLocationIdByName(Server.locationName), Me);
                // }
            }
        }

        /// <summary>
        /// Perform a connection to the server.
        /// </summary>
        public override void Connect() {
            if (Status == Base.Reference.ConnectionStatus.Disconnected) {
                Status = Base.Reference.ConnectionStatus.Connecting;
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
        protected void ConnectionCompleteCallback(IAsyncResult ar) {
            try {
                Socket client = (Socket)ar.AsyncState;
                client.EndConnect(ar);

                client.BeginReceive(GlobalBuffer, 0, GlobalBuffer.Length, SocketFlags.None, DataRecievedCallback, client);

                SendRaw(String.Format("USER {0} 8 * :{0}", Me.Username));

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
        protected void DataRecievedCallback(IAsyncResult ar) {

            Socket recievedOn = (Socket)ar.AsyncState;
            try {
                int recievedAmount = recievedOn.EndReceive(ar);

                byte[] btemp = new byte[recievedAmount];
                Array.Copy(GlobalBuffer, btemp, recievedAmount);

                String text = Encoding.UTF8.GetString(btemp);

                String[] lines = text.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (String line in lines) {
                    String lineTrimmed = line.TrimEnd(new char[] { '\r' }).TrimStart(new char[] { ':' });

                    if (line.EndsWith('\r'.ToString())) {
                        String[] dataChunks = lineTrimmed.Split(new char[] { ' ' });

                        switch (dataChunks.Length) {
                            case 2:
                                if (dataChunks[0].ToLower() == "ping") {
                                    HandlePing(lineTrimmed);
                                }

                                break;

                            default:
                                HandleData(lineTrimmed);
                                break;
                        }
                    } else {
                        byte[] lineBytes = Encoding.UTF8.GetBytes(line);
                        Array.Copy(lineBytes, GlobalBuffer, lineBytes.Length);
                    }
                }

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

        protected override void HandleConnectionComplete(Base.Network n) {
            if (Me.LastDisplayName != "") {
                Base.Message message = new Base.Message(UserIdentifier, Me, "IDENTIFY " + Me.LastDisplayName);
                message.target = new Base.User("NickServ");
                SendMessage(message);
            }

            base.HandleConnectionComplete(n);
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
                        Status = Base.Reference.ConnectionStatus.Connected;
                        HandleConnectionComplete(this);
                        break;

                    case "422":
                        Status = Base.Reference.ConnectionStatus.Connected;
                        HandleConnectionComplete(this);
                        break;

                    case "433":
                        // Nickname in use - do not log because going to identify with LastDisplayName value
                        ChangeNickname(Me.DisplayName + "-", false);
                        break;

                    #endregion

                    case "nick":
                        HandleNicknameChange(line);
                        break;

                    case "join":

                        // TODO: Handle user events
                        Base.User joiner = User.Parse(dataChunks[0]);
                        //if (this.OnUserJoin != null)
                        //    OnUserJoin(this, GetLocationIdByName(dataChunks[2].TrimStart(':')), joiner);
                        break;

                    case "part":
                        Base.User parter = User.Parse(dataChunks[0]);
                        //if (this.OnUserPart != null)
                        //    OnUserPart(this, GetLocationIdByName(dataChunks[2].TrimStart(':')), parter);
                        break;

                    case "quit":
                        Base.User quitter = User.Parse(dataChunks[0]);
                        //if (this.OnUserQuit != null)
                        //    OnUserQuit(this, GetLocationIdByName(Server.locationName), quitter);
                        break;

                    case "mode":
                        HandleModeChange(line);
                        break;


                    case "privmsg":
                    case "notice":
                        Base.Message msg = Message.Parse(this, line);
                        String hostmask = "";
                        string userhost = line.Split(new char[] { ' ' })[0];
                        Match hostmaskMatch = RegularExpressions.USER_REGEX.Match(userhost);
                        if (hostmaskMatch.Success) hostmask = hostmaskMatch.Groups["hostname"].Value;

                        if (Listened.ContainsKey(msg.locationID)) {
                            Base.Location loc = Listened[msg.locationID];

                            if (loc.AccessLevels.ContainsKey("*@" + hostmask))
                                msg.sender.LocalAuthLevel = msg.sender.NetworkAuthLevel = loc.AccessLevels["*@" + hostmask];

                            if (loc.AccessLevels.ContainsKey(msg.sender.DisplayName + "@" + hostmask))
                                msg.sender.LocalAuthLevel = msg.sender.NetworkAuthLevel = loc.AccessLevels[msg.sender.DisplayName + "@" + hostmask];

                            Base.Location serv = Listened[NetworkIdentifier];
                            if (serv.AccessLevels.ContainsKey("*@" + hostmask))
                                msg.sender.NetworkAuthLevel = serv.AccessLevels["*@" + hostmask];

                            if (serv.AccessLevels.ContainsKey(msg.sender.DisplayName + "@" + hostmask))
                                msg.sender.NetworkAuthLevel = serv.AccessLevels[msg.sender.DisplayName + "@" + hostmask];

                            // TODO: Re-do auth levels for user here. Include both server and location level for further processing.
                        }

                        HandleMessageRecieved(this, msg);
                        break;

                    default:
                        Console.WriteLine(line);
                        break;
                }
            }

            catch (Exception e) {
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Join a locationID on the server. This will automatically check the location
        /// password as well, if it is defined it will use it.
        /// </summary>
        /// <param name="locationID">Public (as an Location) to join.</param>
        public override Guid JoinLocation(Networks.Base.Location location) {
            if (Status == Base.Reference.ConnectionStatus.Connected) {
                if (location.Name != null) {
                    Guid loc = GetLocationIdByName(location.Name);

                    // Guid.Empty means location not found - Aka not being listened on yet
                    if (loc == Guid.Empty) {
                        if (location.Password != "") {
                            SendRaw("JOIN " + location.Name + " " + location.Password);
                        } else {
                            SendRaw("JOIN " + location.Name);
                        }

                        Guid newLocationID = Guid.NewGuid();
                        location.Parent = this.NetworkIdentifier;
                        Listened.Add(newLocationID, location);

                        SendRaw("WHO " + location.Name);

                        if (this.OnListeningStart != null) {
                            OnListeningStart(this, newLocationID);
                        }

                        return newLocationID;
                    }
                }
            }

            return Guid.Empty;
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
            if (Status == Base.Reference.ConnectionStatus.Connected) {
                if (locationID != null && locationID != Guid.Empty) {
                    if (Listened.ContainsKey(locationID)) {
                        Base.Location l = Listened[locationID];
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
            if (Status != Base.Reference.ConnectionStatus.Disconnected) {
                byte[] bdata = Encoding.UTF8.GetBytes(data + "\r\n");
                _conn.Send(bdata);
            }
        }

        /// <summary>
        /// Sends a well-formed message, using the message's location as the destination and other
        /// values as expected. The message's SENDER parameter does not matter, but it should
        /// be kept in practice that it matches the bot's current DisplayName on the server.
        /// </summary>
        /// <param name="message">Message to send to the server.</param>
        public override void SendMessage(Base.Message message) {
            if (Status == Base.Reference.ConnectionStatus.Connected && message != null) {

                String location;
                if (Listened.ContainsKey(message.locationID)) {
                    if (message.locationID == UserIdentifier)
                        location = message.target.DisplayName;
                    else
                        location = Listened[message.locationID].Name;

                    switch (message.type) {
                        case Base.Reference.MessageType.PublicMessage:
                        case Base.Reference.MessageType.PrivateMessage:
                        case Base.Reference.MessageType.Unknown:
                            SendRaw("PRIVMSG " + location + " :" + message.message);
                            break;

                        case Base.Reference.MessageType.PublicAction:
                        case Base.Reference.MessageType.PrivateAction:
                            SendRaw("PRIVMSG " + location + " :\u0001ACTION " + message.message + "\u0001");
                            break;

                        case Base.Reference.MessageType.Notice:
                            SendRaw("NOTICE " + location + " :" + message.message);
                            break;

                        default:
                            // Fail.
                            break;
                    }

                    // TODO: Fix events
                    //if (this.OnMessageSent != null)
                    //    OnMessageSent(this, message);
                } else {
                    // Have not joined location
                    Console.WriteLine("Location not available: " + message.locationID);
                }
            }
        }

        /// <summary>
        /// Internal method to handle a NICK line sent from the server.
        /// </summary>
        /// <param name="line">Line to parse.</param>
        protected void HandleNicknameChange(String line) {
            String[] dataChunks = line.Split(new char[] { ' ' });

            Base.User changer = User.Parse(dataChunks[0]);
            changer.LastDisplayName = changer.DisplayName;
            changer.DisplayName = dataChunks[2].TrimStart(':');
                
            if (changer.LastDisplayName == Me.DisplayName) {
                Base.User me = new Base.User(Me.Username, Me.DisplayName, changer.DisplayName);
                Me = me;
                // TODO: Fix Events
                //if (this.OnBotNickChange != null)
                //    OnBotNickChange(this, GetLocationIdByName(Server.locationName), changer);
            } else {
                //if (this.OnUserNickChange != null) {
                //    OnUserNickChange(this, GetLocationIdByName(Server.locationName), changer);
                //}
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
            Status = Base.Reference.ConnectionStatus.Disconnected;
            HandleDisconnectComplete(this);
        }

        private void ParseWhoResponse(String line) {

            /// :foxtaur.furnet.org 352 Delenas #ostenvighx ~Delenas fur-3EB9DC59.hsd1.pa.comcast.net foxtaur.furnet.org Delenas Hr~ :0 Delenas Freshtt
            /// 
            String[] bits = line.Split(new char[] { ' ' });
            Guid locationGuid = GetLocationIdByName(bits[3]);
            User u = new User();
            u.Username = bits[4].TrimStart(new char[]{'~'});
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

        private void HandleModeChange(String line) {
            Match match = Regex.Match(line, RegularExpressions.SENDER_REGEX_RAW + @" MODE " + RegularExpressions.LOCATION_REGEX + @" " + @"(?<data>.*)", RegexOptions.ExplicitCapture);
            if (!match.Success)
                return;

            SendRaw("WHO " + match.Groups["location"].Value);
        }


        /// <summary>
        /// Goes through the temporary who feedback and gets all the hostmasks, translating them into ban strings for the final access list
        /// </summary>
        private void ParseWhoList(String line) {
            String[] lineBits = line.Split(new char[] { ' ' });
            Guid locationID = GetLocationIdByName(lineBits[3]);
            Base.Location location = Listened[locationID];
            location.AccessLevels.Clear();

            foreach(KeyValuePair<string, Dictionary<string, byte>> hostmaskItem in TempUserAccessLevels){
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
    }
}

