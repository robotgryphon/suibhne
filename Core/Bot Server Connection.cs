using System;
using Ostenvighx.Suibhne.Plugins;
using Ostenvighx.Api.Irc;
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
		public delegate void ServerConnectionEvent(BotServerConnection connection);

		public event ServerConnectionEvent OnConnectionComplete;


		public delegate void IrcCommandEvent(BotServerConnection connection, IrcMessage message);

		public event IrcCommandEvent OnCommandRecieved;
		#endregion

		public BotServerConnection(ServerConfig config, PluginRegistry plugins) { 

			this.Configuration = config;

			this.Plugins = plugins;

			this.Connection = new IrcConnection(config.Server);

			this.Plugins.EnablePluginsOnServer(this);

			this.Connection.OnMessageRecieved += HandleMessageRecieved;
			this.Connection.OnConnectionComplete += (conn) => {
				Console.WriteLine("Connection complete on server " + Configuration.Server.hostname);

				if(this.OnConnectionComplete != null){
					OnConnectionComplete(this);
				}

				foreach(IrcLocation location in Configuration.AutoJoinChannels){
					Connection.JoinChannel(location);
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

			IrcMessage response = new IrcMessage(message.location, Connection.CurrentNickname, "Response");
			response.type = MessageType.ChannelMessage;

			switch(command){
				case "test":
					response.message = "Test complete. Probably successful.";
					Connection.SendMessage(response);
					break;

				case "plugins":

					string[] pluginCommandParts = message.message.Split(new char[]{ ' ' }, 3);
					switch(pluginCommandParts.Length) {
						case 1:
							response.message = "Invalid Parameters. Format: !plugins [command]";
							Connection.SendMessage(response);
							break;

						case 2:
							switch(pluginCommandParts[1].ToLower()) {
								case "list":
									int[] activePlugins = Plugins.GetActivePluginsOnServer(this);
									List<String> plugins = new List<string>();
									foreach(int pluginLoopID in activePlugins) {
										plugins.Add(Plugins.GetPlugin(pluginLoopID).Name);
									}

									response.message = "Plugins Active on " + Configuration.FriendlyName + " [" + activePlugins.Length + "]: " + String.Join(", ", plugins);

									Connection.SendMessage(response);

									int[] inactivePluginsArray = Plugins.GetUnactivePluginsOnServer(this);
									if(inactivePluginsArray.Length > 0) {
										List<String> inactivePlugins = new List<string>();
										foreach(int pluginLoopID in inactivePluginsArray) { 
											inactivePlugins.Add(Plugins.GetPlugin(pluginLoopID).Name);	
										}

										response.message = "Plugins Disabled on " + Configuration.FriendlyName + " [" + inactivePluginsArray.Length + "]: " + String.Join(", ", inactivePlugins);
										Connection.SendMessage(response);
									}
									break;

								default:
									response.message = "Unknown command.";
									Connection.SendMessage(response);
									break;

							}
							break;

						case 3:
							int pluginID = -1;
							switch(pluginCommandParts[1].ToLower()) {
								case "serv-enable":
									if(IsBotOperator(message.sender)) {
										pluginID = Plugins.GetPluginID(pluginCommandParts[2]);
										Plugins.EnablePluginOnServer(pluginID, this);
									} else {
										response.message = "You are not a bot operator. No permission to enable and disable plugins.";
										Connection.SendMessage(response);
									}
									break;

								case "serv-disable":
									if(IsBotOperator(message.sender)) {
										pluginID = Plugins.GetPluginID(pluginCommandParts[2]);
										Plugins.DisablePluginOnServer(pluginID, this);
									} else {
										response.message = "You are not a bot operator. No permission to enable and disable plugins.";
										Connection.SendMessage(response);
									}
									break;

								default:
									response.message = "Unknown command.";
									Connection.SendMessage(response);
									break;
							}
							break;
					}

					// End plugins
					break;

				case "raw":
					if(IsBotOperator(message.sender.ToLower())) {
						string rawCommand = message.message.Split(new char[]{ ' ' }, 2)[1];
						Connection.SendRaw(rawCommand);
					} else {
						response.message = "You are not a bot operator. No permission to execute raw commands.";
						Connection.SendMessage(response);
					}
					break;

				default:
					// TODO: Check plugin registry for additional command support here?
					break;
			}
		}

		public void Connect(){
			this.Connection.Connect();
		}

		public void Disconnect(){
			this.Connection.Disconnect();
		}

		protected void HandleMessageRecieved (IrcConnection conn, IrcMessage message)
		{
			Console.WriteLine(message.ToString());

			if(message.message.StartsWith("!"))
				HandleCommand(message);
		}
	}
}

