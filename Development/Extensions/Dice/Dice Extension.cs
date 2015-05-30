﻿
using Ostenvighx.Suibhne.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Nini.Config;

namespace Ostenvighx.Suibhne.Dice {

    class DieRollResult {
        internal string format;
        internal int[] values;
        public long total;

        public override string ToString() {
            return this.total.ToString();
        }
    }

    class DiceExtension : Ostenvighx.Suibhne.Extensions.Extension {

        private static Regex DieFormat = new Regex(@"(?<dice>\d+)d(?<sides>\d+)(?<mod>[\+\-]\d+)?", RegexOptions.None);

        public DiceExtension()
            : base() {
            this.Name = "Dice Roller";
            this.Authors = new string[] { "Ted Senft" };
            this.Version = "1.0.0";
        }

        /// <summary>
        /// Calculates the value of a die roll in standard format.
        /// </summary>
        /// <param name="die">Standard fie rolling format, aka 1d6+4.</param>
        /// <returns></returns>
        protected DieRollResult GetDiceValue(String die) {

            DieRollResult result = new DieRollResult();

            Match dice = DieFormat.Match(die);
            if (dice.Success) {

                result.format = die;

                String numDiceString = dice.Groups["dice"].Value;
                String numSidesString = dice.Groups["sides"].Value;
                String modString = dice.Groups["mod"].Value;

                try {
                    int numDice = int.Parse(numDiceString);
                    int numSides = int.Parse(numSidesString);
                    int mod = 0;

                    result.values = new int[numSides];

                    if (modString != "")
                        mod = int.Parse(modString.TrimStart(new char[] { '+', '-' }));

                    if (numDice >= 1 && numDice <= 500) {
                        if (numSides <= 10000 && numSides >= 1) {
                            if (mod < 5000000) {
                                Random randomizer = new Random();
                                for (int i = 0; i < numDice; i++) {
                                    result.values[i] = randomizer.Next(1, numSides);
                                    if (modString.StartsWith("-")) mod = -mod;
                                    result.values[i] += mod;

                                    result.total += result.values[i];
                                }
                            }
                        }
                    }
                }

                catch (FormatException) {
                    throw;
                }

                catch (Exception) { }
            }

            return result;
        }

        [CommandHandler("rollDice"), Help("Roll up to ten dice, separated by spaces. Format is standard, XdY+Z. Modifier (+Z) can be negative or excluded.")]
        public void DoDiceRoll(Extension ext, Guid connID, String sender, String message) {
            message = message.Trim();

            // If we have no arguments
            if (message == "") {
                ext.SendMessage(connID, Reference.MessageType.ChannelMessage, "Roll up to ten dice, separated by spaces. Format is standard, XdY+Z. Modifier (+Z) can be negative or excluded.");
                return;
            }

            string[] commandParts = message.Split(new char[] { ' ' });
            if (commandParts.Length >= 1 && commandParts.Length <= 10) {

                List<long> rolls = new List<long>();

                for (int die = 1; die < commandParts.Length + 1; die++) {
                    DieRollResult result = GetDiceValue(commandParts[die - 1]);
                    rolls.Add(result.total);
                    result.total = 0;
                }

                long total = rolls.Sum();

                String response = Reference.Bold + Reference.ColorPrefix + "06rolls a few dice, and the results are: " + Reference.Normal + total + "!";

                ext.SendMessage(connID, Reference.MessageType.ChannelMessage, response);
            } else {
                ext.SendMessage(connID, Reference.MessageType.ChannelMessage, "Up to ten dice can be rolled. (You had " + (commandParts.Length - 1) + "). Format is 1d20(+1), up to ten dice (put a space between the dice notations).");
            }
        }

    }
}
