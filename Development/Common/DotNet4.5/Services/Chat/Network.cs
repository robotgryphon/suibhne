using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ostenvighx.Suibhne.Services.Chat {
    public abstract class ChatService : Services.ServiceConnector {

        /// <summary>
        /// The root configuration directory. Passed in from the main application.
        /// </summary>
        protected String ConfigRoot;

        

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
        public Chat.Location Server { get; protected set; }

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
        public event Events.MessageEvent OnMessageRecieved;
        #endregion

        /// <summary>
        /// Get a reference to an Location object by name. Useful for locationID lookups.
        /// </summary>
        /// <param name="locationName">Location to attempt lookup on.</param>
        /// <returns>Reference to the Location for a given locationName.</returns>
        public Guid GetLocationIdByName(String locationName) {
            Guid returned = Guid.Empty;
            foreach (KeyValuePair<Guid, Chat.Location> location in Listened) {
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
        public Chat.Location GetLocationByName(String locationName) {
            Guid locationID = GetLocationIdByName(locationName);
            if (locationID != Guid.Empty)
                return Listened[locationID];

            return Chat.Location.Unknown;
        }

        public abstract void SendMessage(Message m);

        public abstract void JoinLocation(Guid g);

        public abstract void LeaveLocation(Guid g);

        protected virtual void HandleUserJoin(Guid l, User u) {
            
        }

        protected virtual void HandleUserLeave(Guid l, User u) {
            
        }

        protected virtual void HandleUserQuit(Guid l, User u) {
            
        }

        protected virtual void HandleMessageRecieved(Message m, String extraJSON) {
            JObject ev, extra;
            try {
                ev = new JObject();
                extra = JObject.Parse(extraJSON);

                ev.Add("event", "message_recieved");
                ev.Add("location", m.locationID);

                JObject message = new JObject();
                message.Add("contents", m.message);
                if (m.IsPrivate) {
                    message.Add("is_private", true);
                    message.Add("target", m.target.UniqueID);
                }

                ev.Add("message", message);

                JObject sender = new JObject();
                sender.Add("unique_id", m.sender.UniqueID);
                sender.Add("display_name", m.sender.DisplayName);
                ev.Add("sender", sender);

                ev.Merge(extra, new JsonMergeSettings() {
                    MergeArrayHandling = MergeArrayHandling.Union
                });

                base.FireEvent(ev.ToString());
            }

            catch (Exception) {
                Console.WriteLine("Error adding extra json for message; json not valid: " + extraJSON);
            }
        }
    }
}
