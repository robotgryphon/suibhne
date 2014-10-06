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

		public override void Prepare(IrcBot bot) {
			bot.OnCommandRecieved += HandleOnCommandRecieved;
		}

		void HandleOnCommandRecieved (IrcBot bot, IrcMessage message)
		{
			char[] space = new char[] { ' ' };
			Boolean isOperator = bot.IsBotOperator(message.sender);

			String command = message.message.Split(space)[0].ToLower().TrimStart(new char[]{'!'}).Trim();
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
						switch(commandParts.Length) {
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
					} else {
						bot.conn.SendMessage(message.location, "You are not an operator.");
					}

					break;


				case "server":
					if(isOperator) {
						bot.conn.SendMessage(message.location, "Not implemented yet.");
					} else {
						bot.conn.SendMessage(message.location, "You are not an operator.");
					}

					break;

				case "msg":
					if(commandParts.Length >= 3) {
						String msg = message.message.Split(space, 3)[2];
						bot.conn.SendMessage(commandParts[1], msg);
					}

					break;

				case "quit":
					if(isOperator) {
						if(commandParts.Length > 1)
							bot.conn.Disconnect(message.message.Split(space, 2)[1].Trim());
						else
							bot.conn.Disconnect();
					} else {
						bot.conn.SendMessage(message.location, "You are not an operator.");
					}

					break;
			}
		}
	}
}

