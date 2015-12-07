using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ostenvighx.Suibhne.Services.Irc {

    /// <summary>
    /// Contains reference information and commonly-used things for IRC functions.
    /// </summary>
    public struct Reference {

        #region _conn Delegates
        

        /// <summary>
        /// Location events are called when things such as channels and queries are being worked on.
        /// </summary>
        /// <param name="conn">The connection the location event occured on.</param>
        /// <param name="location">A reference to the location calling the event.</param>
        public delegate void IrcLocationEvent(IrcNetwork conn, Guid location);

        /// <summary>
        /// An IRC User event is one fired off when something happens with a user.
        /// Examples are DisplayName changes, joining and parting channels, etc.
        /// </summary>
        /// <param name="conn">Connnection event occurred on.</param>
        /// <param name="location">Where the event is originating from. If server, use the connection's server reference.</param>
        /// <param name="user">User in question.</param>
        public delegate void IrcUserEvent(IrcNetwork conn, Guid location, User user);

        /// <summary>
        /// An Networks.Irc Data event is the lowest-level event, used for ANY type of incoming
        /// or outgoing data. There are probably more focused delegates to use!
        /// </summary>
        public delegate void IrcDataEvent(IrcNetwork conn, String data);
        #endregion
    }
}
