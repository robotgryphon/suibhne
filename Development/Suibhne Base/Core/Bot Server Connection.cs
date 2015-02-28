using System;
using Raindrop.Suibhne.Extensions;
using Raindrop.Api.Irc;
using System.Collections.Generic;

namespace Raindrop.Suibhne.Core {
    public class BotServerConnection {
        public IrcConnection Connection;

        public ServerConfig Configuration;

        public ExtensionRegistry Extensions;

        public byte Identifier { get; protected set; }

        public Boolean Connected {
            get { return Connection.Status == Reference.ConnectionStatus.Connected; }
            protected set { }
        }

        #region Event Handlers

        public delegate void ServerConnectionEvent(BotServerConnection connection);

        public event ServerConnectionEvent OnConnectionComplete;


        public delegate void IrcCommandEvent(BotServerConnection connection, IrcMessage message);

        public event IrcCommandEvent OnCommandRecieved;

        #endregion

        public BotServerConnection(byte id, ServerConfig config, ExtensionRegistry exts) {
            this.Identifier = id;
            this.Configuration = config;

            this.Extensions = exts;

            this.Connection = new IrcConnection(
                config.Hostname,
                config.Port,
                config.Nickname,
                config.Username,
                config.DisplayName,
                config.servPassword,
                config.authPassword);

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
            foreach (String nick in Configuration.Operators)
                if (nick.ToLower() == user.ToLower())
                    return true;

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

