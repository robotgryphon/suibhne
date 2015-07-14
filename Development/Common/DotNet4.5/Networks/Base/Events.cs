﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ostenvighx.Suibhne.Networks.Base {
    public class Events {

        /// <summary>
        /// An Networks.Irc _conn event is one that is typically fired off when the connection is started or stopped.
        /// This is fairly well covered with ConnectionComplete and Disconnect events.
        /// </summary>
        public delegate void NetworkConnectionEvent(Network n);

        /// <summary>
        /// A message event is fired off when some incoming or outgoing form of message
        /// is being handled by the connection. These types of messages include location messages,
        /// queries, various actions, and notices.
        /// 
        /// To get more information, you can look up more information by using the location ID and the firing Network element
        /// to dig up more information.
        /// </summary>
        public delegate void MessageEvent(Message m);

        /// <summary>
        /// A user event is one fired off when something happens with a user.
        /// Examples are DisplayName changes, joining and parting channels, etc.
        /// </summary>
        /// <param name="g">Guid of location event occurred at.</param>
        /// <param name="u">User in question.</param>
        public delegate void UserEvent(Guid g, User u);
    }
}
