using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ostenvighx.Suibhne.Networks.Base {
    public class Location {

        public Guid Parent;

        /// <summary>
        /// A unique location name.
        /// </summary>
        public String Name;

        /// <summary>
        /// The password required to initially connect to the location.
        /// </summary>
        public String Password;

        /// <summary>
        /// Gets the type of location this is.
        /// </summary>
        public Reference.LocationType Type;

        public static Location Unknown {
            get {
                Location e = new Location("Unknown");
                e.Type = Reference.LocationType.Unknown;
                return e;
            }

            private set { }
        }

        public Dictionary<String, byte> AccessLevels;

        /// <summary>
        /// Make a new instance of an Irc Location object.
        /// </summary>
        /// <param name="locationName">Channel name.</param>
        public Location(String locationName)
            : this(locationName, "", Reference.LocationType.Public) {

        }

        /// <summary>
        /// Makes a new instance of an Irc Location object, taking an argument whether it is private or not.
        /// </summary>
        /// <param name="locationName">Location name.</param>
        /// <param name="type">Type of location.</param>
        public Location(String locationName, Reference.LocationType type)
            : this(locationName, "", type) {

        }

        /// <summary>
        /// Makes a new instance of an Irc Location object, taking an optional password as well.
        /// </summary>
        /// <param name="locationName">Location name.</param>
        /// <param name="password">Password to access the location. Used mostly for channels.</param>
        /// <param name="type">Type of location.</param>
        public Location(String locationName, String password, Reference.LocationType type) {
            this.Parent = Guid.Empty;
            this.Name = locationName;
            this.Password = password;
            this.Type = type;

            this.AccessLevels = new Dictionary<string, byte>();
        }

        /// <summary>
        /// Returns base method, because VS won't stop bugging about it.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public override string ToString() {
            return this.Name + " (" + Type + ")";
        }

        /// <summary>
        /// Determines if the location is equal to another.
        /// This is true if the lowercased location name is the same.
        /// </summary>
        /// <param name="obj">Object to compare against.</param>
        /// <returns>True if locationName is equal to the object's locationName. obj must also be of type Location.</returns>
        public override bool Equals(object obj) {
            if (obj.GetType() == typeof(Location)) {
                return this.Name.ToLower() == ((Location)obj).Name.ToLower();
            }

            return false;
        }
    }
}
