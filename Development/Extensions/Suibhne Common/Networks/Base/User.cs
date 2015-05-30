using System;
using System.Text.RegularExpressions;

namespace Ostenvighx.Suibhne.Networks.Base {

    /// <summary>
    /// An Irc User is a user connected to an IRC Network.
    /// This class contains information on their known nicknames,
    /// host mask, and Username.
    /// </summary>
    public class User {

        /// <summary>
        /// The user's Username. This is usually the first bit of their hostmask.
        /// </summary>
        public String Username;

        /// <summary>
        /// The user's last known DisplayName.
        /// If this is the first they've been seen, this is
        /// set to a default value of their original seen DisplayName.
        /// </summary>
        public String LastDisplayName;

        /// <summary>
        /// The user's current known DisplayName. Kept as up to date
        /// as possible when they change nicknames.
        /// </summary>
        public String DisplayName;

        public User() : this("unknown") { }

        /// <summary>
        /// Create a new User, specifying the DisplayName.
        /// </summary>
        /// <param name="DisplayName">Current DisplayName of user.</param>
        public User(String nickname) 
            :this(nickname, nickname, nickname) { }

        public User(String username, String last_displayname, String current_displayname) {
            this.Username = username;
            this.LastDisplayName = last_displayname;
            this.DisplayName = current_displayname;
        }


        /// <summary>
        /// Returns a string that is equal to the User's current DisplayName.
        /// </summary>
        /// <returns>User.DisplayName</returns>
        public override string ToString() {
            return this.DisplayName;
        }
    }
}

