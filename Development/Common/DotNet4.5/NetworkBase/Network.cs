using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ostenvighx.Suibhne.Networks.Base {
    public abstract class Network {

        /// <summary>
        /// The root configuration directory. Passed in from the main application.
        /// </summary>
        protected String ConfigRoot;

        /// <summary>
        /// A value that indicates what status the connection is in.
        /// See the Base.Reference.ConnectionStatus enum.
        /// </summary>
        /// <seealso cref="Ostenvighx.Suibhne.Networks.Base.Reference.ConnectionStatus"/>
        public Base.Reference.ConnectionStatus Status;

        /// <summary>
        /// A MessageRecieved event is fired off when an Message is recieved on the server.
        /// This includes notices, queries, actions, and regular locationID messages.
        /// </summary>
        public event Events.MessageEvent OnMessageRecieved;

        /// <summary>
        /// A MessageSent event is fired off when an Message is sent to the server.
        /// This includes notices, queries, actions, and regular locationID messages.
        /// </summary>
        public event Events.MessageEvent OnMessageSent;

        /// <summary>
        /// Occurs when a connection is complete and data is ready to be served.
        /// </summary>
        public event Events.NetworkConnectionEvent OnConnectionComplete;

        /// <summary>
        /// Occurs when a connection is completely terminated.
        /// </summary>
        public event Events.NetworkConnectionEvent OnDisconnectComplete;

        #region User Events
        /// <summary>
        /// Called when a user joins a locationID the connection is listening on.
        /// </summary>
        public event Events.UserEvent OnUserJoin;

        /// <summary>
        /// Called when a user parts a locationID the connection is listening on.
        /// </summary>
        public event Events.UserEvent OnUserLeave;

        /// <summary>
        /// Called when a user quits the server the connection is at.
        /// </summary>
        public event Events.UserEvent OnUserQuit;

        /// <summary>
        /// Called when a user changes their DisplayName on the server.
        /// </summary>
        public event Events.UserEvent OnUserDisplayNameChange;

        /// <summary>
        /// Called when the connection's DisplayName changes, through a 433 code or manually.
        /// </summary>
        public event Events.UserEvent OnBotNickChange;

        #endregion
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

        /// <summary>
        /// A list of all the listened locations.
        /// This list contains joined channels.
        /// </summary>
        public Dictionary<Guid, Location> Listened {
            get;
            protected set;
        }

        public abstract void Setup(string configFile);

        #region Event Methods
        public abstract bool IsEventSupported(String eventName);
        public abstract String[] GetSupportedEvents();
        #endregion

        #region Connection Methods
        public abstract void Connect();
        public abstract void Disconnect(String reason);
        #endregion

        /// <summary>
        /// Get a reference to an Location object by name. Useful for locationID lookups.
        /// </summary>
        /// <param name="locationName">Location to attempt lookup on.</param>
        /// <returns>Reference to the Location for a given locationName.</returns>
        public Guid GetLocationIdByName(String locationName) {
            Guid returned = Guid.Empty;
            foreach (KeyValuePair<Guid, Base.Location> location in Listened) {
                if (location.Value.Name.Equals(locationName.ToLower()))
                    return location.Key;
            }

            return returned;
        }

        /// <summary>
        /// Get a reference to an Location object by name. Useful for locationID lookups.
        /// </summary>
        /// <param name="locationName">Location to attempt lookup on.</param>
        /// <returns>Reference to the Location for a given locationName.</returns>
        public Base.Location GetLocationByName(String locationName) {
            Guid locationID = GetLocationIdByName(locationName);
            if (locationID != Guid.Empty)
                return Listened[locationID];

            return Base.Location.Unknown;
        }

        public abstract void SendMessage(Message m);

        public abstract void JoinLocation(Guid g);

        public abstract void LeaveLocation(Guid g);

        #region User Events
        protected virtual void HandleUserJoin(Guid l, User u) {
            if (this.OnUserJoin != null)
                OnUserJoin(l, u);
        }

        protected virtual void HandleUserLeave(Guid l, User u) {
            if (this.OnUserLeave != null)
                OnUserLeave(l, u);
        }

        protected virtual void HandleUserQuit(Guid l, User u) {
            if (this.OnUserQuit != null)
                OnUserQuit(l, u);
        }

        protected virtual void HandleUserDisplayNameChange(Guid l, User u) {
            if (this.OnUserDisplayNameChange != null)
                OnUserDisplayNameChange(l, u);
        }
        #endregion

        #region Network Events
        protected virtual void HandleConnectionComplete(Network n) {
            if (n.OnConnectionComplete != null)
                n.OnConnectionComplete(n);
        }

        protected virtual void HandleDisconnectComplete(Network n) {
            if (n.OnDisconnectComplete != null)
                n.OnDisconnectComplete(n);
        }
        
        protected virtual void HandleMessageRecieved(Message m) {
            if (OnMessageRecieved != null)
                OnMessageRecieved(m);
        }
        #endregion
    }
}
