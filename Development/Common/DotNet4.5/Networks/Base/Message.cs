
using System;
using System.Runtime.Serialization;
using System.Text;
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

        public Message(byte[] stream) {
            // <destination:16>
            // <messageType:1>
            // <message:*> = {sender} {message}
            byte[] guidBytes = new byte[16];

            Array.Copy(stream, 0, guidBytes, 0, 16);
            this.locationID = new Guid(guidBytes);

            this.type = (Ostenvighx.Suibhne.Networks.Base.Reference.MessageType) stream[16];

            byte[] messageBytes = new byte[stream.Length - 17];
            Array.Copy(stream, 17, messageBytes, 0, messageBytes.Length);
            this.message = Encoding.UTF8.GetString(messageBytes);

            sender = new User(message.Split(';')[0]);
            target = new User(message.Split(';')[1]);
            message = message.Split(';')[2];
        }

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

        public static Message GenerateResponse(User u, Message msg) {
            Message response = new Message(msg.locationID, u, "Response");
            response.type = Reference.MessageType.PublicMessage;

            if (Message.IsPrivateMessage(msg)) {
                response.type = Reference.MessageType.PrivateMessage;
                response.target = msg.sender;
            }

            return response;
        }

        public static bool IsPrivateMessage(Message msg) {
            switch (msg.type) {
                case Reference.MessageType.PrivateMessage:
                case Reference.MessageType.PrivateAction:
                case Reference.MessageType.Notice:
                    return true;

                default:
                    return false;
            }
        }

        public byte[] ConvertToBytes() {
            byte[] messageAsBytes = Encoding.UTF8.GetBytes(this.sender.DisplayName + ";" + this.target.DisplayName + ";" + message);
            byte[] rawMessage = new byte[17 + messageAsBytes.Length];

            // Copy message's target location to array
            Array.Copy(locationID.ToByteArray(), 0, rawMessage, 0, 16);

            // Copy in message type
            rawMessage[16] = (byte) type;

            // Copy message sender and message bytes in
            Array.Copy(messageAsBytes, 0, rawMessage, 17, messageAsBytes.Length);

            return rawMessage;
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

