using System;
using Ostenvighx.Suibhne.Plugins;
using Ostenvighx.Suibhne.Core;
using Ostenvighx.Api.Networking.Irc;


namespace Ostenvighx.Suibhne.CorePlugins {

	public class BasicCommands : PluginBase {

		public BasicCommands() {
			this.Name = "Basic Commands";
			this.Author = "Ted Senft";
		}
			
		public override void EnableOnServer(BotServerConnection server) {
			server.OnCommandRecieved += HandleOnCommandRecieved;
		}

		public override void DisableOnServer(BotServerConnection server) {
			server.OnCommandRecieved -= HandleOnCommandRecieved;
		}
			
		void HandleOnCommandRecieved (BotServerConnection server, IrcMessage message)
		{
			char[] space = new char[] { ' ' };
			Boolean isOperator = server.IsBotOperator(message.sender);

			String command = message.message.Split(space)[0].ToLower().TrimStart(new char[]{'!'}).Trim();
			String[] commandParts = message.message.Split(space);

			// This will soon be handled by the plugin registry

			switch(command) {
				case "join":
					if(isOperator)
						server.Connection.JoinChannel(new IrcChannel(commandParts[1]));
					else
						server.Connection.SendMessage(new IrcMessage(message.location, "", "You are not an operator."));

					break;

				case "part":
					if(isOperator) {
						switch(commandParts.Length) {
							case 2:
								server.Connection.PartChannel(commandParts[1]);
								break;

							case 3:
								String reason = message.message.Split(space, 3)[2];
								server.Connection.PartChannel(commandParts[1], reason);
								break;

							default:
								server.Connection.SendMessage(message.sender, "Not enough parameters. Need the channel to leave.");
								break;
						}
					} else {
						server.Connection.SendMessage(message.location, "You are not an operator.");
					}

					break;


				case "server":
					if(isOperator) {
						server.Connection.SendMessage(message.location, "Not implemented yet.");
					} else {
						server.Connection.SendMessage(message.location, "You are not an operator.");
					}

					break;

				case "msg":
					if(commandParts.Length >= 3) {
						String msg = message.message.Split(space, 3)[2];
						server.Connection.SendMessage(commandParts[1], msg);
					}

					break;

				case "act":
					if(commandParts.Length >= 3) {
						String msg = message.message.Split(space, 3)[2];
						IrcMessage msgMessage = new IrcMessage(commandParts[1], "Suibhne", msg);
						msgMessage.isAction = true;

						server.Connection.SendMessage(msgMessage);
					}

					break;

				case "quit":
					if(isOperator) {
						if(commandParts.Length > 1)
							server.Connection.Disconnect(message.message.Split(space, 2)[1].Trim());
						else
							server.Connection.Disconnect();
					} else {
						server.Connection.SendMessage(message.location, "You are not an operator.");
					}

					break;
			}
		}
	}
}

