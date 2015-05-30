using System;
using System.IO;
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

        /// <summary>
        /// A list of all the listened locations.
        /// This list contains joined channels.
        /// </summary>
        protected Dictionary<Guid, Base.Location> Listened;

        /// <summary>
        /// A container used for temporarily storing users from a NAMES list.
        /// </summary>
        private Dictionary<String, List<Base.User>> TempUsersContainer;

        #region _conn Events
        /// <summary>
        /// Occurs when on connection complete.
        /// </summary>
        public event Reference.IrcConnectionEvent OnConnectionComplete;

        /// <summary>
        /// Occurs when a connection is terminated.
        /// </summary>
        public event Reference.IrcConnectionEvent OnDisconnectComplete;
        #endregion

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
        /// Create a new Networks.Irc _conn object using the default Networks.Irc Configuration.
        /// The default Networks.Irc Configuration attempts to connect to a local server (on the host machine).
        /// </summary>
        public IrcNetwork() {
            this._conn = null;
            this.GlobalBuffer = new byte[2048];

            this.Listened = new Dictionary<Guid, Base.Location>();
            this.Status = Base.Reference.ConnectionStatus.Disconnected;

            this._conn = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            this.TempUsersContainer = new Dictionary<string, List<Base.User>>();
            this.UserIdentifier = Guid.NewGuid();
            this.Listened.Add(UserIdentifier, new Base.Location("<user>"));

            this.Me = new Base.User();
            this.Server = new Base.Location("localhost", Networks.Base.Reference.LocationType.Network);
            this.Listened.Add(Guid.NewGuid(), Server);

            this.port = 6667;

            this.OnConnectionComplete += HandleFinishConnection;

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
            this.Me = new Base.User();
            Me.Username = username;
            Me.LastDisplayName = authPass;
            Me.DisplayName = nickname;

            this.Server = new Base.Location(host, password, Base.Reference.LocationType.Network);
            this.port = port;
        }

        /// <summary>
        /// Create a new IRC _conn object with the specified configuration values.
        /// </summary>
        /// <param name="config">Configuration to use to connect to the server.</param>
        public IrcNetwork(IConfig config)
            : this() {

            // Initialize Me variable with "known" information.
            this.Me = new Base.User(
                config.GetString("username", "user"),
                config.GetString("authpassword", ""),
                config.GetString("nickname", "IrcUser"));

            this.Server = new Base.Location(
                config.GetString("host", "localhost"),
                config.GetString("password", ""),
                Base.Reference.LocationType.Network);

            this.Listened.Add(Guid.NewGuid(), Server);

            this.port = config.GetInt("port", 6667);
        }

        protected virtual void HandleFinishConnection(IrcNetwork conn) {

            if (Me.LastDisplayName != "") {
                Base.Message message = new Base.Message(UserIdentifier, Me, "IDENTIFY " + Me.LastDisplayName);
                message.target = new Base.User("NickServ");
                SendMessage(message);
            }

            this.OnConnectionComplete -= HandleFinishConnection;
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

                    _conn.BeginConnect(Server.locationName, 6667, new AsyncCallback(ConnectionCompleteCallback), _conn);
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
            switch (dataChunks[1].ToLower()) {

                #region Numeric Codes
                case "001":
                    // Network welcome message
                    // Network.locationName = dataChunks[0].TrimStart(new char[] { ':' });
                    break;

                case "353":
                case "366":
                    #region Names response
                    String chan = "#channel";
                    switch (dataChunks[1].ToLower()) {

                        case "353":
                            chan = line.Split(new char[] { ' ' })[4].Trim().ToLower();

                            String[] namesList = line.Split(new String[] { "353" }, StringSplitOptions.None)[1].Split(new char[] { ':' }, 2)[1].Trim().Split(new char[] { ' ' });

                            if (!TempUsersContainer.ContainsKey(chan))
                                TempUsersContainer.Add(chan, new List<Base.User>());

                            foreach (String user in namesList)
                                TempUsersContainer[chan].Add(new Base.User(user));
                            break;

                        case "366":
                            chan = line.Split(new char[] { ' ' })[3].Trim().ToLower();
                            if (TempUsersContainer.ContainsKey(chan)) {

                                // Return this?
                                // TempUsersContainer[chan].ToArray()
                                TempUsersContainer.Remove(chan);
                            }
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
                    if (this.OnConnectionComplete != null)
                        OnConnectionComplete(this);
                    break;

                case "422":
                    Status = Base.Reference.ConnectionStatus.Connected;
                    if (this.OnConnectionComplete != null)
                        OnConnectionComplete(this);
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

                case "privmsg":
                case "notice":
                    Base.Message msg = Message.Parse(this, line);

                    //if (this.OnMessageRecieved != null)
                    //    this.OnMessageRecieved(this, msg);
                    break;
            }
        }

        /// <summary>
        /// Get a reference to an Location object by name. Useful for locationID lookups.
        /// </summary>
        /// <param name="locationName">Location to attempt lookup on.</param>
        /// <returns>Reference to the Location for a given locationName.</returns>
        public Guid GetLocationIdByName(String locationName) {
            Guid returned = Guid.Empty;
            foreach (KeyValuePair<Guid, Base.Location> location in Listened) {
                if (location.Value.locationName.Equals(locationName.ToLower()))
                    return location.Key;
            }

            return returned;
        }

        /// <summary>
        /// Get a reference to an Location object by name. Useful for locationID lookups.
        /// </summary>
        /// <param name="locationName">Location to attempt lookup on.</param>
        /// <returns>Reference to the Location for a given locationName.</returns>
        public Base.Location GetLocationByName(String locationName) {
            Guid locationID = GetLocationIdByName(locationName);
            if (locationID != Guid.Empty)
                return Listened[locationID];

            return Base.Location.Unknown;
        }

        /// <summary>
        /// Join a locationID on the server. This will automatically check the location
        /// password as well, if it is defined it will use it.
        /// </summary>
        /// <param name="locationID">Public (as an Location) to join.</param>
        public override Guid JoinLocation(Networks.Base.Location location) {
            if (Status == Base.Reference.ConnectionStatus.Connected) {
                if (location.locationName != null) {
                    Guid loc = GetLocationIdByName(location.locationName);

                    // Guid.Empty means location not found - Aka not being listened on yet
                    if (loc == Guid.Empty) {
                        if (location.password != "") {
                            SendRaw("JOIN " + location.locationName + " " + location.password);
                        } else {
                            SendRaw("JOIN " + location.locationName);
                        }

                        Guid newLocationID = Guid.NewGuid();
                        Listened.Add(newLocationID, location);

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

                        SendRaw("PART " + Listened[locationID].locationName + " :" + reason);
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
                        location = Listened[message.locationID].locationName;

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

            if (this.OnDisconnectComplete != null)
                OnDisconnectComplete(this);
        }

    }
}

