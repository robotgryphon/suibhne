using System;
using System.Net;
using System.IO;
using System.Collections.Generic;

using Ostenvighx.Api.Networking;
using Ostenvighx.Api.Networking.Irc;

using Ostenvighx.Suibhne.Core;

namespace Ostenvighx.Suibhne {

	public class Launch {

		public static void Main(String[] args) {

			IrcConfig fuzzies = new IrcConfig("localhost", 6667, "Suibhne", "Suibhne", "Suibhne", "Suibhne");

			IrcBot bot = new IrcBot(fuzzies);
			bot.OnCommandRecieved += HandleOnCommandRecieved;

			// Add delenas to the bot operator list (will soon change to connection op list)
			bot.Operators.Add("delenas");

			// Add #ostenvighx to the autojoin list
			bot.Autojoin.Add("#ostenvighx");

			bot.Connect();



			// Keep the bot alive until user hits any key
			while(true) { }
		}

		static void HandleOnCommandRecieved(IrcBot bot, IrcMessage message) {
			String command = message.message.Split(new char[]{ ' ' }, 2)[0].Substring(1).Trim().ToLower();
			Boolean isOperator = bot.IsBotOperator(message.sender);
			char[] space = new char[] { ' ' };
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

				default:
					// moduleSystem.CheckCommand(command, message);

					bot.conn.SendMessage(message.location, "Unrecognized command.");
					break;
			}

		}
	}
}

