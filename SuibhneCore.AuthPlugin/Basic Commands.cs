using System;
using Ostenvighx.Suibhne.Plugins;
using Ostenvighx.Suibhne.Core;
using Ostenvighx.Api.Networking.Irc;

namespace Suibhne.CorePlugins {
	public class BasicCommands : PluginBase {

		public BasicCommands() {
			this.Name = "Basic Commands";
			this.Author = "Ted Senft";
		}

		public override void Prepare(IrcBot bot) {
			bot.OnCommandRecieved += HandleOnCommandRecieved;
		}

		void HandleOnCommandRecieved (IrcBot bot, IrcMessage message)
		{
			char[] space = new char[] { ' ' };
			Boolean isOperator = bot.IsBotOperator(message.sender);

			String command = message.message.Split(space, 2)[0].Substring(1).Trim().ToLower();
			String[] commandParts = message.message.Split(space);

			// This will soon be handled by the plugin registry

			switch(command) {
				case "join":
					if(isOperator)
						bot.conn.JoinChannel(commandParts[1]);
					else
						bot.conn.SendMessage(message.location, "You are not an operator.");

					break;

				case "part":
					if(isOperator) {
						switch(commandParts.Length){
							case 2:
								bot.conn.PartChannel(commandParts[1]);
								break;

							case 3:
								String reason = message.message.Split(space, 3)[2];
								bot.conn.PartChannel(commandParts[1], reason);
								break;

							default:
								bot.conn.SendMessage(message.sender, "Not enough parameters. Need the channel to leave.");
								break;
						}
					} else
						bot.conn.SendMessage(message.location, "You are not an operator.");

					break;


				case "server":
					if(isOperator) {
						bot.conn.SendMessage(message.location, "Not implemented yet.");
					} else
						bot.conn.SendMessage(message.location, "You are not an operator.");

					break;

				case "quit":
					if(isOperator) {
						switch(commandParts.Length){
							case 2:
								bot.conn.Disconnect(commandParts[1]);
								break;

							default:
								bot.conn.Disconnect();

								break;
						}
					} else
						bot.conn.SendMessage(message.location, "You are not an operator.");

					break;
			}
		}
	}
}

