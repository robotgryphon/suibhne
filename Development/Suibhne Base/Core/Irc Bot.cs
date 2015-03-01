
using System;
using System.Net;
using System.IO;
using System.Collections.Generic;

using Raindrop.Api.Irc;

using Raindrop.Suibhne.Extensions;

using System.Net.Sockets;
using System.Threading;

namespace Raindrop.Suibhne.Core {

	public class IrcBot {

		public Dictionary<byte, BotServerConnection> Connections;

		public IrcBotConfiguration Configuration;

		public ExtensionRegistry Extensions { get; protected set; }

		public byte ConnectedCount { get; protected set; }

        public event Reference.IrcMessageEvent OnMessageRecieved;

        public event BotServerConnection.IrcCommandEvent OnCommandRecieved;

		public IrcBot() {
			this.Connections = new Dictionary<byte, BotServerConnection>();
			this.Configuration = IrcBotConfiguration.LoadFromFile(Environment.CurrentDirectory + "/Suibhne.ini");
			this.ConnectedCount = 0;

			this.Extensions = new ExtensionRegistry(this);
		}

		public void LoadServers(){
			foreach(String serverName in Configuration.Servers) {

				try {
					ServerConfig sc = (ServerConfig) ServerConfig.LoadFromFile(Configuration.ConfigDirectory + "Servers/" + serverName + "/" + serverName + ".ini");
					BotServerConnection conn = new BotServerConnection(ConnectedCount++, sc, Extensions);
					AddConnection(conn);
				}

				catch(Exception e){
					Console.WriteLine(e);
				}
			}
		}

		public void AddConnection(BotServerConnection connection) {

			this.Connections.Add(connection.Identifier, connection);
            connection.Connection.OnMessageRecieved += (conn, msg) => {
                if (this.OnMessageRecieved != null) {
                    OnMessageRecieved(conn, msg);
                }
            };

            connection.OnCommandRecieved += (conn, msg) => {
                if (this.OnCommandRecieved != null)
                    OnCommandRecieved(conn, msg);
            };
		}

		public void Start(){
			foreach(KeyValuePair<byte, BotServerConnection> conn in Connections) {
				conn.Value.Connect();
			}
		}

        public void Stop() {
            foreach (KeyValuePair<byte, BotServerConnection> conn in Connections) {
                conn.Value.Disconnect();
            }

            Connections.Clear();
        }
	}
}

