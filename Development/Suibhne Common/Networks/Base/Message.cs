
using System;
using System.Text.RegularExpressions;

namespace Ostenvighx.Suibhne.Networks.Base {

    /// <summary>
    /// An Networks.Irc Message handles all incoming and outgoing privmsg data on servers. Use it to
    /// parse incoming messages, queries, notices, and the like.
    /// </summary>
    public class Message {

        /// <summary>
        /// The inbound or outbound location of the message.
        /// </summary>
        public Guid locationID;

        /// <summary>
        /// Only used when the message needs to go to an individual, instead of a channel or server.
        /// </summary>
        public User target;

        /// <summary>
        /// Who sent the message.
        /// </summary>
        public User sender;

        /// <summary>
        /// The actual message contents, stripped out of the data.
        /// </summary>
        public String message;

        /// <summary>
        /// The type of message being handled.
        /// </summary>
        public Reference.MessageType type;

        /// <summary>
        /// Create an instance of a new Networks.Irc Message object.
        /// Default message type is unknown.
        /// </summary>
        /// <param name="location">Location of the message.</param>
        /// <param name="sender">Sender of message.</param>
        /// <param name="message">Body of message recieved/sent.</param>
        public Message(Guid location, User sender, String message) {
            this.target = new User("unknown");
            this.sender = sender;
            this.locationID = location;
            this.message = message;
            this.type = Reference.MessageType.Unknown;
        }
   

        /// <summary>
        /// Output an example of a formatted Message. This will give subtle hints on the type of message, and shows
        /// all of the contents (sender, message, and location).
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="Ostenvighx.Suibhne.Networks.Irc.Message"/>.</returns>
        public override string ToString() {
            switch (type) {
                case Reference.MessageType.PublicMessage:
                case Reference.MessageType.PrivateMessage:
                    return String.Format("[{0}] <{1}> {2}", locationID, sender, message);

                case Reference.MessageType.PublicAction:
                case Reference.MessageType.PrivateAction:
                    return String.Format("[{0}] * {1} {2}", locationID, sender, message);

                case Reference.MessageType.Notice:
                    return String.Format("-{0}- {1}", sender, message);

                default:
                    return String.Format("[{0}] <{1}> {2}", locationID, sender, message);
            }
        }
    }
}

