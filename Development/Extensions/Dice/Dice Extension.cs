
using Raindrop.Suibhne.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Raindrop.Suibhne.Dice {
    class DiceExtension : Extensions.Extension {

        public static Regex DieFormat = new Regex(@"(?<dice>\d+)d(?<sides>\d+)(?<mod>[\+\-]\d+)?", RegexOptions.None);

        public DiceExtension() : base() {
            this.Name = "Dice Roller";
            this.Authors = new string[] { "Ted Senft" };
            this.Version = "1.0.0";
        }

        /// <summary>
        /// Calculates the value of a die roll in standard format.
        /// </summary>
        /// <param name="die">Standard fie rolling format, aka 1d6+4.</param>
        /// <returns></returns>
        protected long GetDiceValue(String die) {
            long diceTotal = 0;

            Match dice = DieFormat.Match(die);
            if (dice.Success) {

                String numDiceString = dice.Groups["dice"].Value;
                String numSidesString = dice.Groups["sides"].Value;
                String modString = dice.Groups["mod"].Value;

                try {
                    int numDice = int.Parse(numDiceString);
                    int numSides = int.Parse(numSidesString);
                    int mod = 0;

                    if (modString != "")
                        mod = int.Parse(modString.TrimStart(new char[] { '+', '-' }));

                    if (numDice >= 1 && numDice <= 500) {
                        if (numSides <= 10000 && numSides >= 1) {
                            if (mod < 5000000) {

                                int total = 0;

                                Random randomizer = new Random();
                                for (int i = 0; i < numDice; i++) {
                                    total += randomizer.Next(1, numSides);
                                }

                                if (modString.StartsWith("-"))
                                    mod = -mod;

                                total += mod;
                                diceTotal += total;
                            } else
                                throw new FormatException("Modifier must be less than 5 million.");
                        } else
                            throw new FormatException("Number of sides must be between 1 and 10 thousand.");
                    } else
                        throw new FormatException("Can only roll 1 to 500 dice at a time.");
                }

                catch (FormatException) {
                    throw new FormatException();
                }

                catch (Exception) { }
            }

            return diceTotal;
        }

        public string DoDiceRoll(String command) {
            string[] commandParts = command.Split(new char[] { ' ' });
            string message = "";

            if (command == "roll" || command == "dice") {

                long total = 0;
                if (commandParts.Length >= 2 && commandParts.Length <= 11) {

                    int invalidDice = 0;

                    for (int die = 1; die < commandParts.Length; die++) {
                        try {
                            total += GetDiceValue(commandParts[die]);
                        }

                        catch (FormatException fe) {
                            invalidDice++;
                        }
                    }

                    message = "\u0002\u000306Results\u000f" + ((invalidDice > 0) ? " (Some were in an invalid format)" : "") + ": " + total;
                } else {
                    message = "Up to ten dice can be rolled. (You had " + (commandParts.Length - 1) + "). Format is 1d20(+1), up to ten dice (put a space between the dice notations).";
                }
            }

            return message;
        }

    }
}
