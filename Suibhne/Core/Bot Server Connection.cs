using System;
using Ostenvighx.Suibhne.Plugins;
using Ostenvighx.Api.Networking.Irc;
using System.Collections.Generic;

namespace Ostenvighx.Suibhne.Core {
	public class BotServerConnection {
		public IrcConnection Connection;

		public ServerConfig Configuration;

		public PluginRegistry Plugins;

		public Boolean Connected {
			get { return Connection.Connected; }
			protected set { }
		}

		#region Event Handlers
		public delegate void ServerConnectionEvent(BotServerConnection connection, EventArgs args);

		public event ServerConnectionEvent OnConnectionComplete;


		public delegate void IrcCommandEvent(BotServerConnection connection, IrcMessage message);

		public event IrcCommandEvent OnCommandRecieved;
		#endregion

		public BotServerConnection(ServerConfig config, PluginRegistry plugins) { 

			this.Configuration = config;

			this.Plugins = plugins;

			this.Connection = new IrcConnection(config.Server);

			this.Plugins.RegisterPluginSets(config.Plugins);
			this.Plugins.EnablePluginsFromList(this);

			this.Connection.OnMessageRecieved += HandleMessageRecieved;
			this.Connection.OnConnectionComplete += (conn, args) => {
				Console.WriteLine("Connection complete on server " + Configuration.Server.hostname);

				if(this.OnConnectionComplete != null){
					OnConnectionComplete(this, EventArgs.Empty);
				}

				foreach(IrcChannel channel in Configuration.AutoJoinChannels){
					Connection.JoinChannel(channel);
				}
			};
		}

		public Boolean IsBotOperator(String user) {
			return Configuration.Operators.Contains(user.ToLower());
		}

		protected void HandleCommand(IrcMessage message) {
			if(this.OnCommandRecieved != null) {
				OnCommandRecieved(this, message);
			}

			String command = message.message.Split(new char[]{ ' ' })[0].ToLower().TrimStart(new char[]{'!'}).TrimEnd();
			Console.WriteLine("Command recieved from " + message.sender + ": " + command);

			if(command == "plugins") {
				string[] pluginCommandParts = message.message.Split(new char[]{ ' ' }, 3);
				switch(pluginCommandParts.Length) {
					case 1:
						Connection.SendMessage(message.location, "Invalid Parameters. Format: !plugins [command]");
						break;

					case 2:
						switch(pluginCommandParts[1].ToLower()) {
							case "list":
								List<PluginBase> activePlugins = Plugins.GetActivePluginsOnServer(this);
								List<String> plugins = new List<string>();

								foreach(PluginBase p in activePlugins) {
									plugins.Add(p.Name);
								}

								Connection.SendMessage(message.location, "Plugins Active on " + Configuration.FriendlyName + ": " + String.Join(", ", plugins));
								break;

							default:
								Connection.SendMessage(message.location, "Unknown command.");
								break;

						}

						break;

					case 3:

						break;

				}
			}
		}

		public void Connect(){
			this.Connection.Connect();
		}

		public void Disconnect(){
			this.Connection.Disconnect();
		}

		protected void HandleMessageRecieved (IrcConnection conn, IrcMessage message, EventArgs args)
		{
			Console.WriteLine(message.ToString());

			if(message.message.StartsWith("!"))
				HandleCommand(message);
		}
	}
}

