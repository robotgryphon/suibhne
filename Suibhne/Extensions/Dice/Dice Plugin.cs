using System;
using Ostenvighx.Api.Irc;

using Ostenvighx.Suibhne.Core;
using Ostenvighx.Suibhne.Plugins;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Ostenvighx.Suibhne.DicePlugin {
	public class DicePlugin : PluginBase {
		public DicePlugin() : base() {
			this.Name = "Dice";
			this.Author = "Ted Senft";
		}

		public override void EnableOnServer(BotServerConnection server) {
			server.OnCommandRecieved += HandleOnCommandRecieved;
		}

		public override void DisableOnServer(BotServerConnection server) {
			server.OnCommandRecieved -= HandleOnCommandRecieved;
		}

		private void HandleOnCommandRecieved (BotServerConnection server, IrcMessage message)
		{
			char[] space = new char[] { ' ' };

			String command = message.message.Split(space)[0].ToLower().TrimStart(new char[]{'!'}).Trim();
			String[] commandParts = message.message.Trim().Split(space);

			// server.Connection.SendMessage(message.location, "Command length: " + commandParts.Length);
			if(command == "roll" || command == "dice") {

				IrcMessage response = new IrcMessage(message.location, server.Connection.CurrentNickname, "");
				response.type = MessageType.ChannelMessage;

				if(commandParts.Length >= 2 && commandParts.Length <= 11) {

					List<String> diceResults = new List<String>();
					int invalidDice = 0;
					long diceTotal = 0;

					for(int die = 1; die < commandParts.Length; die++) {
						Regex diceFormat = new Regex(@"(?<dice>\d+)d(?<sides>\d+)(?<mod>[\+\-]\d+)?", RegexOptions.None);
						Match dice = diceFormat.Match(commandParts[die]);
						if(dice.Success) {

							String numDiceString = dice.Groups["dice"].Value;
							String numSidesString = dice.Groups["sides"].Value;
							String modString = dice.Groups["mod"].Value;

							try {
								int numDice = int.Parse(numDiceString);
								int numSides = int.Parse(numSidesString);
								int mod = 0;

								if(modString != "")
									mod = int.Parse(modString.TrimStart(new char[]{ '+', '-' }));

								int total = 0;

								if(numDice >= 1 && numDice <= 500) {
									if(numSides <= 10000 && numSides >= 1) {
										if(mod < 5000000) {
											Random randomizer = new Random();
											for(int i = 0; i < numDice; i++) {
												total += randomizer.Next(1, numSides);
											}

											if(modString.StartsWith("-"))
												mod = -mod;

											total += mod;

											diceTotal += total;

											diceResults.Add("\u0002" + numDiceString + "d" + numSidesString + modString + ":\u000f " + total.ToString());
										} else {
											invalidDice++;
										}
									} else {
										invalidDice++;
									}
								} else {

								}

							} catch(FormatException) {
								invalidDice++;
							} catch(Exception) {
								invalidDice++;
							}
						} else {
							// Fall through, this die was invalid
							invalidDice++;
						}
					}

					response.message = "\u0002\u000306Results\u000f" + ((invalidDice > 0) ? " (Some were in an invalid format)" : "") + ": " + diceTotal + " (" + String.Join("\u0002 - \u000f ", diceResults) + ")";
				} else {
					response.message = "Up to ten dice can be rolled. (You had " + (commandParts.Length - 1) + "). Format is 1d20(+1), up to ten dice (put a space between the dice notations).";
				}

				server.Connection.SendMessage(response);
			}
		}
	}
}

