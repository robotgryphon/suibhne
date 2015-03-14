using System;
using Raindrop.Suibhne.Extensions;
using Raindrop.Api.Irc;
using System.Collections.Generic;

namespace Raindrop.Suibhne {
    public class IrcBot {
        public IrcConnection Connection;

        private ServerConfig Configuration;

        protected ExtensionRegistry Extensions;

        public Guid Identifier { get; protected set; }

        public Boolean Connected {
            get { return Connection.Status == IrcReference.ConnectionStatus.Connected; }
            protected set { }
        }

        #region Event Handlers

        public delegate void ServerConnectionEvent(IrcBot connection);

        public event ServerConnectionEvent OnConnectionComplete;


        public delegate void IrcCommandEvent(IrcBot connection, IrcMessage message);

        public event IrcCommandEvent OnCommandRecieved;

        #endregion

        public IrcBot(ServerConfig config, ExtensionRegistry exts) {
            this.Identifier = Guid.NewGuid();
            this.Configuration = config;

            this.Extensions = exts;

            this.Connection = new IrcConnection(
                config.Hostname,
                config.Port,
                config.Nickname,
                config.Username,
                config.DisplayName,
                config.ServPassword,
                config.AuthPassword);

            this.Connection.OnMessageRecieved += HandleMessageRecieved;
            this.Connection.OnConnectionComplete += (conn) => {
                Console.WriteLine("Connection complete on server " + Configuration.Hostname);

                if (this.OnConnectionComplete != null) {
                    OnConnectionComplete(this);
                }

                foreach (IrcLocation location in Configuration.AutoJoinChannels) {
                    Connection.JoinChannel(location);
                }
            };
        }

        public Boolean IsBotOperator(String user) {
            // TODO: Get status code from nickserv + channel level

            return false;
        }

        protected void HandleCommand(IrcMessage message) {
            if (this.OnCommandRecieved != null) {
                OnCommandRecieved(this, message);
            }

            Extensions.HandleCommand(this, message);
        }

        public void Connect() {
            this.Connection.Connect();
        }

        public void Disconnect() {
            this.Connection.Disconnect();
        }

        protected void HandleMessageRecieved(IrcConnection conn, IrcMessage message) {
            Console.WriteLine("Bot connection handling message: " + message.ToString());

            if (message.message.StartsWith("!"))
                HandleCommand(message);
        }
    }
}

