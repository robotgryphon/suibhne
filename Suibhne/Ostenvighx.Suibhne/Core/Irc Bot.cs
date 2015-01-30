
using System;
using System.Net;
using System.IO;
using System.Collections.Generic;

using Ostenvighx.Api.Irc;

using Ostenvighx.Suibhne.Extensions;

using System.Net.Sockets;
using System.Threading;

namespace Ostenvighx.Suibhne.Core {

	public class IrcBot {

		public Dictionary<String, BotServerConnection> Connections;

		public IrcBotConfiguration Configuration;

		public ExtensionRegistry Extensions { get; protected set; }

		public int ConnectedCount { get; protected set; }

		public IrcBot() {
			this.Connections = new Dictionary<string, BotServerConnection>();
			this.Configuration = IrcBotConfiguration.LoadFromFile(Environment.CurrentDirectory + "/Suibhne.ini");
			this.ConnectedCount = 0;

			this.Extensions = new ExtensionRegistry(this);
		}

		public void LoadServers(){
			foreach(String serverName in Configuration.Servers) {

				try {
					ServerConfig sc = (ServerConfig) ServerConfig.LoadFromFile(Configuration.ConfigDirectory + "Servers/" + serverName + "/" + serverName + ".ini");
					BotServerConnection conn = new BotServerConnection(sc, Extensions);
					AddConnection(serverName, conn);
				}

				catch(Exception e){
					Console.WriteLine(e);
				}
			}
		}

		public void AddConnection(String connID, BotServerConnection connection) {

			if(!this.Connections.ContainsKey(connID)) {
				this.Connections.Add(connID, connection);
			} else {
				throw new Exception("That server is already in the list");
			}
		}

		public void Start(){
			foreach(KeyValuePair<String, BotServerConnection> conn in Connections) {
				conn.Value.Connect();
			}
		}
	}
}

