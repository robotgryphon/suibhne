
using System;
using System.Net;
using System.IO;
using System.Collections.Generic;

using Ostenvighx.Api.Networking;
using Ostenvighx.Api.Networking.Irc;

using Ostenvighx.Suibhne.Plugins;

using System.Net.Sockets;
using System.Threading;

namespace Ostenvighx.Suibhne.Core {

	public class IrcBot {

		public Dictionary<String, BotServerConnection> Connections;

		public IrcBotConfiguration Configuration;

		public PluginRegistry Plugins { get; protected set; }

		public IrcBot() {
			this.Connections = new Dictionary<string, BotServerConnection>();
			this.Configuration = new IrcBotConfiguration();
			this.Configuration.LoadFrom(Environment.CurrentDirectory + "/Configuration/Bot.json");

			this.Plugins = new PluginRegistry(this, Configuration.ConfigDirectory + "Plugins/");
			Console.WriteLine(Plugins.PluginDirectory);

		}

		public void AddConnection(String connID, BotServerConnection connection) {

			if(!this.Connections.ContainsKey(connID)) {
				this.Connections.Add(connID, connection);
				connection.Connect();
			}
		}
	}
}

