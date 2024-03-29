﻿
using System;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;

namespace Ostenvighx.Suibhne.Services.Chat {

    public class Message {

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

        public Boolean IsPrivate;

        public Message(String message) : this(null, message) {

        }

        /// <summary>
        /// Create an instance of a new Networks.Irc Message object.
        /// Default message type is unknown.
        /// </summary>
        /// <param name="location">Location of the message.</param>
        /// <param name="sender">Sender of message.</param>
        /// <param name="message">Body of message recieved/sent.</param>
        public Message(User sender, String message) {
            this.target = new User("unknown");
            this.sender = sender;
            this.message = message;
        }

        public static Message GenerateResponse(Message msg) {
            Message response = new Message("Response");

            if (msg.IsPrivate) {
                response.IsPrivate = true;
                response.target = msg.sender;
            }

            return response;
        }
        
        /// <summary>
        /// Output an example of a formatted Message. This will give subtle hints on the type of message, and shows
        /// all of the contents (sender, message, and location).
        /// </summary>
        /// <returns>A <see cref="System.String"/> that represents the current <see cref="Ostenvighx.Suibhne.Services.Irc.Message"/>.</returns>
        public override string ToString() {
            return String.Format("{2}{0}{3} {1}", 
                sender, 
                message, 
                IsPrivate ? "-" : "<", IsPrivate ? "-" : ">"
            );
        }
    }
}

