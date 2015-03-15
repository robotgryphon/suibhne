using System;
using Raindrop.Suibhne.Extensions;
using Raindrop.Api.Irc;
using System.Collections.Generic;

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

        public IrcBot(ServerConfig config, ExtensionSystem exts)
            : base(config.Hostname,
                config.Port,
                config.Nickname,
                config.Username,
                config.DisplayName,
                config.ServPassword,
                config.AuthPassword) {

            this.Identifier = Guid.NewGuid();
            this.Configuration = config;
            this.Operators = new List<string>();
            foreach (String op in config.Operators)
                Operators.Add(op.ToLower());

            this.Extensions = exts;

            this.OnMessageRecieved += HandleMessageRecieved;
            this.OnConnectionComplete += (conn) => {
                Console.WriteLine("Connection complete on server " + Configuration.Hostname);

                foreach (Location location in Configuration.AutoJoinChannels) {
                    JoinChannel(location);
                }
            };
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
            if (message.message.StartsWith("!"))
                HandleCommand(message);
        }
    }
}

