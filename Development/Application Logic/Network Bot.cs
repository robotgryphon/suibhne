using System;
using Ostenvighx.Suibhne.Extensions;
using System.Collections.Generic;
using System.IO;
using Nini.Config;
using Ostenvighx.Suibhne.Networks.Base;
using System.Reflection;
using System.Data;

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

        // public String FriendlyName { get; private set; }

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
            try {
                this.Identifier = Guid.Parse(configDir.Substring(configDir.LastIndexOf("/") + 1));
            }

            catch (Exception) {
                return;
            }


            if (!File.Exists(configDir + "/network.ini")) {
                Core.Log("Could not load network information file: " + configDir + "/network.ini");
                return;
            }

            IniConfigSource config = new IniConfigSource(configDir + "/network.ini");
            config.CaseSensitive = false;

            if (config.Configs["Network"] == null || config.Configs["Network"].Get("type") == null)
                throw new KeyNotFoundException("Network configuration file missing network section. Cannot continue.");

            string networkType = config.Configs["Network"].GetString("type", "unknown");

            if (networkType != null && networkType != "" && networkType != "unknown") {
                
                // First file should be network dll
                Assembly networkAssembly = Assembly.LoadFrom(Core.ConfigDirectory + "/Connectors/" + networkType + "/" + networkType + ".dll");
                Type[] types = networkAssembly.GetTypes();
                foreach (Type t in types) {
                    if (t.IsSubclassOf(typeof(Network))) {
                        this._network = (Network)Activator.CreateInstance(t);
                        _network.Setup(config.SavePath);
                        _network.Listened.Add(Identifier, new Location("<network>", Networks.Base.Reference.LocationType.Network));
                        _network.OnMessageRecieved += this.HandleMessageRecieved;
                        _network.OnConnectionComplete += (conn) => {
                            AutoJoinLocations();
                        };
                    }
                }

                // TODO: Tear this out of here, check in database instead
                // foreach(String opIdentifier in config.Configs["Operators"].GetKeys()){
                //    _network.Listened[Identifier].AccessLevels.Add(opIdentifier, (byte) config.Configs["Operators"].GetInt(opIdentifier));
                // }
            }

            this.Status = Networks.Base.Reference.ConnectionStatus.Disconnected;
        }

        public void ReloadConfiguration() {
            try {
                String file = Core.ConfigDirectory + "/Networks/" + this.Identifier + "/network.ini";

                _network.Setup(file);
            }

            catch (Exception) {
                Core.Log("There was an error processing the configuration reload for network " + this.Identifier);
                return;
            }

        }

        public void SendMessage(Message m) {
            this._network.SendMessage(m);
        }

        protected void AutoJoinLocations() {
            DataTable locations = LocationManager.GetChildLocations(this.Identifier);
            if (locations == null)
                return;

            foreach (DataRow l in locations.Rows) {
                try {

                    Core.Log("Attempting to join " + l["Name"].ToString() + " on network.");

                    Guid newLocationID = Guid.Parse(l["Identifier"].ToString());
                    _network.JoinLocation(newLocationID);
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
            // TODO: Make the command character configurable?
            if (message.message.StartsWith("!"))
                HandleCommand(message);
        }

        public void Connect() {
            if (this._network == null)
                return;

            _network.Connect();
        }

        public void Disconnect(string reason = "Suibhne system shutting down.") {
            _network.Disconnect(reason);
        }

        public int CompareTo(object obj) {
            if (obj.GetType() != typeof(NetworkBot))
                throw new ArgumentException("Object is not a network");

            return ((NetworkBot)obj).Identifier.CompareTo(this.Identifier);
        }
    }
}

