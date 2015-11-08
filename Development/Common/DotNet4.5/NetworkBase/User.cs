using Newtonsoft.Json.Linq;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace Ostenvighx.Suibhne.Networks.Base {

    public class User {

        public enum AccessLevel : byte {
            Unassigned = 0,
            Basic = 1,
            
            Authenticated = 100,

            BotAdmin = 250
        }

        public byte NetworkAuthLevel;
        public byte LocalAuthLevel;

        /// <summary>
        /// The user's Username. This is usually the first bit of their hostmask.
        /// </summary>
        public String UniqueName;

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

        public User(byte[] stream) {
            String base64 = Encoding.UTF8.GetString(stream);
            byte[] converted = Convert.FromBase64String(base64);
            String decoded = Encoding.UTF8.GetString(converted);


            JObject thisAsJson = JObject.Parse(decoded);
            this.UniqueName = thisAsJson["Username"].ToString();
            this.LastDisplayName = thisAsJson["LastDisplayName"].ToString();
            this.DisplayName = thisAsJson["DisplayName"].ToString();
        }

        public User() : this("unknown") { }

        /// <summary>
        /// Create a new User, specifying the DisplayName.
        /// </summary>
        /// <param name="DisplayName">Current DisplayName of user.</param>
        public User(String nickname) 
            :this(nickname, nickname, nickname) { }

        public User(String username, String last_displayname, String current_displayname) {
            this.NetworkAuthLevel = (byte)User.AccessLevel.Basic; 
            this.LocalAuthLevel = (byte)User.AccessLevel.Basic;
            this.UniqueName = username;
            this.LastDisplayName = last_displayname;
            this.DisplayName = current_displayname;
        }

        public byte[] ConvertToBytes() {
            JObject thisAsJson = new JObject();
            thisAsJson.Add("DisplayName", this.DisplayName);
            thisAsJson.Add("LastDisplayName", this.LastDisplayName);
            thisAsJson.Add("Username", this.UniqueName);

            String json = thisAsJson.ToString();
            byte[] data = Encoding.UTF8.GetBytes(json);
            String base64 = Convert.ToBase64String(data);
            return Encoding.UTF8.GetBytes(base64);
        }

        /// <summary>
        /// Returns a string that is equal to the User's current DisplayName.
        /// </summary>
        /// <returns>User.DisplayName</returns>
        public override string ToString() {
            return this.DisplayName;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public override bool Equals(object obj) {
            if (obj.GetType() == typeof(User)) {
                return ((User)obj).UniqueName == this.UniqueName;
            }

            return false;
        } 
    }
}

