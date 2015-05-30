﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ostenvighx.Suibhne.Networks.Base {
    public class Location {
        /// <summary>
        /// A lowercased form of the location in question. A locationID name or user DisplayName.
        /// </summary>
        public String locationName;

        /// <summary>
        /// The password required to initially connect to the location.
        /// </summary>
        public String password;

        /// <summary>
        /// Gets the type of location this is.
        /// </summary>
        public Reference.LocationType type;

        public static Location Unknown {
            get {
                Location e = new Location("Unknown");
                e.type = Reference.LocationType.Unknown;
                return e;
            }

            private set { }
        }

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
            this.locationName = locationName;
            this.password = password;
            this.type = type;
        }

        /// <summary>
        /// Returns base method, because VS won't stop bugging about it.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() {
            return base.GetHashCode();
        }

        /// <summary>
        /// Determines if the location is equal to another.
        /// This is true if the lowercased location name is the same.
        /// </summary>
        /// <param name="obj">Object to compare against.</param>
        /// <returns>True if locationName is equal to the object's locationName. obj must also be of type Location.</returns>
        public override bool Equals(object obj) {
            if (obj.GetType() == typeof(Location)) {
                return this.locationName.ToLower() == ((Location)obj).locationName.ToLower();
            }

            return false;
        }
    }
}
