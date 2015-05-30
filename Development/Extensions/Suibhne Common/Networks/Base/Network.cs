using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ostenvighx.Suibhne.Networks.Base {
    public abstract class Network {

        /// <summary>
        /// A value that indicates what status the connection is in.
        /// See the Base.Reference.ConnectionStatus enum.
        /// </summary>
        /// <seealso cref="Ostenvighx.Suibhne.Networks.Base.Reference.ConnectionStatus"/>
        public Base.Reference.ConnectionStatus Status {
            get;
            protected set;
        }

        /// <summary>
        /// A MessageRecieved event is fired off when an Message is recieved on the server.
        /// This includes notices, queries, actions, and regular locationID messages.
        /// </summary>
        public event Events.NetworkMessageEvent OnMessageRecieved;

        /// <summary>
        /// A MessageSent event is fired off when an Message is sent to the server.
        /// This includes notices, queries, actions, and regular locationID messages.
        /// </summary>
        public event Events.NetworkMessageEvent OnMessageSent;

        /// <summary>
        /// Includes known information about the bot as an Networks.Base User.
        /// Use DisplayName and similar information here.
        /// </summary>
        public User Me { get; protected set; }

        /// <summary>
        /// The last known details of the connected server, as an Location object.
        /// This really shouldn't change, but.. that bridge can be crossed later.
        /// </summary>
        /// <value>The server hostname.</value>
        public Base.Location Server { get; protected set; }

        public abstract void Setup(string configFile);

        #region Connection Methods
        public abstract void Connect();
        public abstract void Disconnect(String reason);
        #endregion

        public abstract void SendMessage(Message m);

        public abstract Guid JoinLocation(Location l);

        public abstract void LeaveLocation(Guid g);
    }
}
