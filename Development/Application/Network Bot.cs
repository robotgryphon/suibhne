using System;
using Ostenvighx.Suibhne.Extensions;
using System.Collections.Generic;
using System.IO;
using Nini.Config;
using Ostenvighx.Suibhne.Networks.Base;
using System.Reflection;

namespace Ostenvighx.Suibhne {
    public class NetworkBot : IComparable {
        protected Network _network;

        public Network Network {
            get {
                return this._network;
            }

            protected set { }
        }

        public Networks.Base.Reference.ConnectionStatus Status {
            get {
                return (_network != null) ? _network.Status : Networks.Base.Reference.ConnectionStatus.NotReady;
            }

            protected set {
                _network.Status = value;
            }
        }

        public String FriendlyName {
            get;
            private set;
        }

        public User Me {
            get { return _network.Me; }
            protected set { }
        }

        public Guid Identifier { get; protected set; }

        #region Event Handlers
        public delegate void IrcCommandEvent(NetworkBot connection, Message message);
        public event IrcCommandEvent OnCommandRecieved;
        #endregion

        public NetworkBot(String configDir){
            this.FriendlyName = configDir.Substring(configDir.LastIndexOf("/") + 1);

            if (!File.Exists(configDir + "/" + FriendlyName + ".ini")) {
                Core.Log("Could not load network information file: " + configDir + "/" + FriendlyName + ".ini");
                return;
            }

            IniConfigSource config = new IniConfigSource(configDir + "/" + FriendlyName + ".ini");
            config.CaseSensitive = false;


            this.Identifier = Guid.Parse((String) Utilities.GetLocationEntry(FriendlyName)["Identifier"]);

            string networkType = config.Configs["Network"].GetString("type", "unknown");

            if (networkType != "unknown") {

                String networkBase = Directory.GetParent(configDir).Parent.FullName + @"\NetworkTypes\";
                string[] files = Directory.GetFiles(networkBase + "/", networkType + ".dll");
                
                // First file should be network dll
                Assembly networkAssembly = Assembly.LoadFrom(files[0]);
                Type[] types = networkAssembly.GetTypes();
                foreach (Type t in types) {
                    if (t.IsSubclassOf(typeof(Network))) {
                        this._network = (Network)Activator.CreateInstance(t);
                        _network.Setup(config.SavePath);
                        _network.Listened.Add(Identifier, new Location("<network>", Networks.Base.Reference.LocationType.Network));
                        _network.OnMessageRecieved += this.HandleMessageRecieved;
                        _network.OnConnectionComplete += (conn) => {
                            AutoJoinLocations(configDir);
                        };
                    }
                }

                foreach(String opIdentifier in config.Configs["Operators"].GetKeys()){
                    _network.Listened[Identifier].AccessLevels.Add(opIdentifier, (byte) config.Configs["Operators"].GetInt(opIdentifier));

                }
            }

            this.Status = Networks.Base.Reference.ConnectionStatus.Disconnected;
        }

        public bool IsListeningTo(Guid g) {
            foreach (Guid listened in _network.Listened.Keys) {
                if (listened == g) {
                    return true;
                }
            }

            return false;
        }

        public void SendMessage(Message m) {
            this._network.SendMessage(m);
        }

        protected void AutoJoinLocations(String configDir) {
            String[] locations = Directory.GetDirectories(configDir + "/Locations/");

            foreach (String location in locations) {
                try {
                    String locationName = location.Substring(location.LastIndexOf("/") + 1);
                    IniConfigSource locConfig = new IniConfigSource(location + "/" + locationName + ".ini");
                    Ostenvighx.Suibhne.Networks.Base.Location loc = new Ostenvighx.Suibhne.Networks.Base.Location(
                        locConfig.Configs["Location"].GetString("Name", "#Location"),
                        Networks.Base.Reference.LocationType.Public);

                    Guid newLocationID = Guid.Parse((String) Utilities.GetLocationEntry(FriendlyName, loc.Name)["Identifier"]);
                    
                    _network.JoinLocation(newLocationID, loc);
                }

                catch (Exception e) {
                    Core.Log("Location loading failed: " + e.Message, LogType.ERROR);
                }
            }
        }

        protected void HandleCommand(Message message) {
            if (this.OnCommandRecieved != null) {
                OnCommandRecieved(this, message);
            }

           ExtensionSystem.Instance.HandleCommand(this, message);
        }

        protected void HandleMessageRecieved(Message message) {
            Core.Log(message.ToString(), LogType.INCOMING);
            if (message.message.StartsWith("!"))
                HandleCommand(message);
        }

        public void Connect() {
            _network.Connect();
        }

        public void Disconnect() {
            _network.Disconnect("Suibhne system shutdown.");
        }

        public int CompareTo(object obj) {
            if (obj.GetType() != typeof(NetworkBot))
                throw new ArgumentException("Object is not a network");

            return ((NetworkBot)obj).FriendlyName.CompareTo(this.FriendlyName);
        }
    }
}

