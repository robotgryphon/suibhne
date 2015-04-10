using System;
using Raindrop.Suibhne.Extensions;
using Raindrop.Api.Irc;
using System.Collections.Generic;
using System.IO;
using Nini.Config;

namespace Raindrop.Suibhne {
    public class IrcBot : Raindrop.Api.Irc.Connection {

        private ServerConfig Configuration;

        protected ExtensionSystem Extensions;

        public Guid Identifier { get; protected set; }

        public List<string> Operators {
            get;
            protected set;
        }

        #region Event Handlers
        public delegate void IrcCommandEvent(IrcBot connection, Message message);
        public event IrcCommandEvent OnCommandRecieved;
        #endregion

        public IrcBot(String configDir, ExtensionSystem exts) : base() {
            this.Identifier = Guid.NewGuid();
            this.Configuration = ServerConfig.LoadFromFile(configDir + "/connection.ini");
            this.Server = new Location(Configuration.Hostname, Configuration.ServPassword, Api.Irc.Reference.LocationType.Server);
            this.port = Configuration.Port;
            this.Me = new User("", Configuration.Username, Configuration.AuthPassword, Configuration.Nickname);

            this.Operators = new List<string>();
            foreach (String op in Configuration.Operators)
                Operators.Add(op.ToLower());

            this.Extensions = exts;

            this.OnMessageRecieved += HandleMessageRecieved;
            this.OnConnectionComplete += (conn) => {
                Core.Log("Connection complete on server " + Configuration.Hostname, LogType.GENERAL);
                AutoJoinLocations(configDir);
            };

            exts.AddBot(this);
        }

        protected void AutoJoinLocations(String configDir) {
            String[] locations = Directory.GetFiles(configDir + "/Locations/", "*.ini");
            foreach (String location in locations) {
                try {
                    IniConfigSource locConfig = new IniConfigSource(location);
                    Location loc = new Location(locConfig.Configs["Location"].GetString("Name", "#Location"), Api.Irc.Reference.LocationType.Channel);

                    JoinChannel(loc);
                }

                catch (Exception e) {
                    Core.Log("Location loading failed: " + e.Message, LogType.ERROR);
                }
            }
        }

        public Boolean IsBotOperator(String user) {
            return this.Operators.Contains(user.ToLower());
        }

        protected void HandleCommand(Message message) {
            if (this.OnCommandRecieved != null) {
                OnCommandRecieved(this, message);
            }

            Extensions.HandleCommand(this, message);
        }

        protected void HandleMessageRecieved(Connection conn, Message message) {
            Core.Log(message.ToString(), LogType.INCOMING);
            if (message.message.StartsWith("!"))
                HandleCommand(message);
        }
    }
}

