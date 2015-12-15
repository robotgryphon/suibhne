
using System;
using System.Text.RegularExpressions;

namespace Ostenvighx.Suibhne.Services.Irc {

    /// <summary>
    /// An Networks.Irc Message handles all incoming and outgoing privmsg data on servers. Use it to
    /// parse incoming messages, queries, notices, and the like.
    /// </summary>
    public class Message : Chat.Message {
        internal String location;
        public Boolean IsAction;

        public Message(User sender, String message)
            : base(sender, message) {
            location = "";
        }

        /// <summary>
        /// Take a well-formed PRIVMSG or NOTICE line and parse it into its various components.
        /// </summary>
        /// <param name="conn">_conn object. Used to determine if the message is a private query.</param>
        /// <param name="line">Line to parse.</param>
        public static Message Parse(IrcNetwork conn, String line) {

           string[] bits = line.Split(new char[]{' '});
            if(bits[1].ToLower() == "notice" || bits[1].ToLower() == "privmsg") {
                // Check if sender is server or a user on said server
                User sender = User.Parse(bits[0]);

                Message msg = new Message(
                    sender,
                    line.Substring(line.IndexOf(" :") + 2));

                msg.location = bits[2] == "*" ? "<server>" : bits[2];

                if (bits[1].ToLower() == "notice")
                    msg.IsPrivate = true;

                // If message is an action (does check in property) then strip the action stuff off the message
                if (msg.message.StartsWith("\u0001ACTION ")) {
                    msg.IsAction = true;
                    msg.message = msg.message.Remove(msg.message.LastIndexOf("\u0001")).Remove(0, "\u0001ACTION ".Length).Trim();
                }

                // If message origin is the bot's current Username, add 2 to type to signal Private type.
                if (msg.location.ToLower() == conn.Me.DisplayName.ToLower()) {
                    msg.IsPrivate = true;
                    msg.location = msg.sender.DisplayName;
                    msg.target = msg.sender;
                }

                return msg;

            } else {
                Console.WriteLine(line);
                throw new FormatException("Invalid message given.");
            }
        }
    }
}

