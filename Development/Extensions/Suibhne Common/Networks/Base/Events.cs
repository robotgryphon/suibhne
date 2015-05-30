using System;
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
        /// An IRC Message event is fired off when some incoming or outgoing form of message
        /// is being handled by the connection. These types of messages include locationID messages,
        /// queries, various actions, and notices.
        /// </summary>
        public delegate void NetworkMessageEvent(Network n, Message m);
    }
}
