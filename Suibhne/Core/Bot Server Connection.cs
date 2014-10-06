using System;
using Ostenvighx.Suibhne.Plugins;
using Ostenvighx.Api.Networking.Irc;
using System.Collections.Generic;

namespace Ostenvighx.Suibhne {
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

			this.Plugins.RegisterPlugins(config.Plugins);
			this.Plugins.EnablePlugins(this);


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

