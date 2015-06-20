using System;
using Ostenvighx.Suibhne.Extensions;
using System.Collections.Generic;
using System.IO;
using Nini.Config;
using Ostenvighx.Suibhne.Networks.Base;
using System.Reflection;

namespace Ostenvighx.Suibhne {
    public class NetworkBot {

        protected ExtensionSystem _extensions;
        protected Network _network;

        public User Me {
            get { return _network.Me; }
            protected set { }
        }

        public Guid Identifier { get; protected set; }

        #region Event Handlers
        public delegate void IrcCommandEvent(NetworkBot connection, Message message);
        public event IrcCommandEvent OnCommandRecieved;
        #endregion

        public NetworkBot(String configDir, ExtensionSystem exts){
            this.Identifier = Guid.NewGuid();
            this._extensions = exts;

            IniConfigSource config = new IniConfigSource(configDir + "/connection.ini");
            config.CaseSensitive = false;

            string networkType = config.Configs["Network"].GetString("type", "unknown");

            if (networkType != "unknown") {

                // Fun happens here. Reflection to create a new network from an assembly.
                // Oh god, what have I done...
                String networkBase = Directory.GetParent(configDir).Parent.FullName + @"\NetworkTypes\";
                string[] files = Directory.GetFiles(networkBase + "/", networkType + ".dll");
                
                // First file should be network dll
                Assembly networkAssembly = Assembly.LoadFrom(files[0]);
                Type[] types = networkAssembly.GetTypes();
                foreach (Type t in types) {
                    if (t.IsSubclassOf(typeof(Network))) {
                        this._network = (Network)Activator.CreateInstance(t);
                        _network.Setup(configDir + "/connection.ini");
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

            exts.AddBot(this);
        }

        public void SendMessage(Message m) {
            this._network.SendMessage(m);
        }

        protected void AutoJoinLocations(String configDir) {
            String[] locations = Directory.GetFiles(configDir + "/Locations/", "*.ini");
            foreach (String location in locations) {
                try {
                    IniConfigSource locConfig = new IniConfigSource(location);
                    Ostenvighx.Suibhne.Networks.Base.Location loc = new Ostenvighx.Suibhne.Networks.Base.Location(
                        locConfig.Configs["Location"].GetString("Name", "#Location"),
                        Networks.Base.Reference.LocationType.Public);

                    Guid id = _network.JoinLocation(loc);
                    Core.NetworkLocationMap.Add(id, Identifier);
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

            _extensions.HandleCommand(this, message);
        }

        protected void HandleMessageRecieved(Network conn, Message message) {
            Core.Log(message.ToString(), LogType.INCOMING);
            if (message.message.StartsWith("!"))
                HandleCommand(message);
        }

        public void Connect() {
            _network.Connect();
        }
    }
}

