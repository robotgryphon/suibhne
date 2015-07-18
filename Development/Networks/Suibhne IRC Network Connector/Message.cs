
using System;
using System.Text.RegularExpressions;

namespace Ostenvighx.Suibhne.Networks.Irc {

    /// <summary>
    /// An Networks.Irc Message handles all incoming and outgoing privmsg data on servers. Use it to
    /// parse incoming messages, queries, notices, and the like.
    /// </summary>
    public class Message : Base.Message {

        public Message(Guid locationID, User sender, String message)
            : base(locationID, sender, message) { }

        /// <summary>
        /// Take a well-formed PRIVMSG or NOTICE line and parse it into its various components.
        /// </summary>
        /// <param name="conn">_conn object. Used to determine if the message is a private query.</param>
        /// <param name="line">Line to parse.</param>
        public static Base.Message Parse(IrcNetwork conn, String line) {

           string[] bits = line.Split(new char[]{' '});
            if(bits[1].ToLower() == "notice" || bits[1].ToLower() == "privmsg") {
                // Check if sender is server or a user on said server
                Base.User sender = User.Parse(bits[0]);

                Base.Message msg = new Base.Message(
                    conn.GetLocationIdByName(bits[2].ToLower()),
                    sender,
                    line.Substring(line.IndexOf(" :") + 2));

                
                msg.type = Base.Reference.MessageType.PublicMessage;

                if (bits[1].ToLower() == "notice")
                    msg.type = Base.Reference.MessageType.Notice;

                // If an action, add 1 to the type value to get it to the Action type.
                if (msg.message.StartsWith("\u0001ACTION ")) {
                    msg.type += 1;
                    msg.message = msg.message.Remove(msg.message.LastIndexOf("\u0001")).Remove(0, "\u0001ACTION ".Length).Trim();
                }

                // If message origin is the bot's current Username, add 2 to type to signal Private type.
                if (msg.locationID == Guid.Empty && bits[2].ToLower() == conn.Me.DisplayName.ToLower()) {
                    if(msg.type != Base.Reference.MessageType.Notice) msg.type += 2;
                    msg.locationID = conn.Identifier;
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

