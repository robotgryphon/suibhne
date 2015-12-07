using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ostenvighx.Suibhne.Services.Chat {

    /// <summary>
    /// Contains reference information and commonly-used things for IRC functions.
    /// </summary>
    public struct Reference {

        #region Enumerations
        

        /// <summary>
        /// Defines various types of messages on a server.
        /// Used in the Message class to handle output and routing.
        /// </summary>
        public enum MessageType : byte {

            /// <summary>
            /// A message being sent in a public locationID.
            /// </summary>
            PublicMessage = 1,

            /// <summary>
            /// A message that is being performed as an action in a public locationID.
            /// </summary>
            PublicAction = 2,

            /// <summary>
            /// A regular message that was sent in a query window.
            /// </summary>
            PrivateMessage = 3,

            /// <summary>
            /// An action being performed in a query window.
            /// </summary>
            PrivateAction = 4,

            /// <summary>
            /// A message being sent as a notice to a user.
            /// </summary>
            Notice = 5,

            /// <summary>
            /// This message type has not been identified yet. Probably the case that
            /// the Message object was just created and has not been parsed yet. If an
            /// unknown message type is sent with the connection, it is treated as a public
            /// message.
            /// </summary>
            Unknown = 0
        }

        /// <summary>
        /// Lookup for what kind of location the object is referring to.
        /// Can be a location, query, notice.. etc.
        /// </summary>
        public enum LocationType : byte {
            /// <summary>
            /// Location is a serverwide broadcast.
            /// </summary>
            Network = 1,

            /// <summary>
            /// Location is a locationID.
            /// </summary>
            Public = 2,

            /// <summary>
            /// Location is a private query window.
            /// </summary>
            Private = 3,

            /// <summary>
            /// Location is a notice recieved by a user or locationID.
            /// </summary>
            Notice = 4,

            /// <summary>
            /// Use if not sure. You really should be sure, though.
            /// </summary>
            Unknown = 0
        };

        #endregion

        
    }
}
