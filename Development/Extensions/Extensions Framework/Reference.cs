using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Raindrop.Suibhne.Extensions {
    public struct Reference {

        /// <summary>
        /// Defines various types of messages on a server.
        /// Used in the IrcMessage class to handle output and routing.
        /// </summary>
        public enum MessageType : byte {

            /// <summary>
            /// A message being sent in a public channel.
            /// </summary>
            ChannelMessage = 1,

            /// <summary>
            /// A message that is being performed as an action in a public channel.
            /// </summary>
            ChannelAction = 2,

            /// <summary>
            /// A regular message that was sent in a query window.
            /// </summary>
            QueryMessage = 3,

            /// <summary>
            /// An action being performed in a query window.
            /// </summary>
            QueryAction = 4,

            /// <summary>
            /// A message being sent as a notice to a user.
            /// </summary>
            Notice = 5,

            /// <summary>
            /// This message type has not been identified yet. Probably the case that
            /// the Message object was just created and has not been parsed yet. If an
            /// unknown message type is sent with the connection, it is treated as a channel
            /// message.
            /// </summary>
            Unknown = 0
        }

        public static String Bold = "\u0002";
        public static String Italic = "\u001D";
        public static String Underline = "\u001F";
        public static String Normal = "\u000f";
        public static String ColorPrefix = "\u0003";

        public static Regex MessageResponseParser = new Regex(@"^(?<location>[^\s]+)\s(?<sender>[^\s]+)\s(?<message>.*)$");
    }
}
