using System;
using System.Text.RegularExpressions;

namespace Ostenvighx.Suibhne.Networks.Irc {

    /// <summary>
    /// An Networks.Irc User is a user connected to an IRC Network.
    /// This class contains information on their known nicknames,
    /// host mask, and Username.
    /// </summary>
    public class User : Ostenvighx.Suibhne.Networks.Base.User {

        /// <summary>
        /// An array containing all valid operator prefixes.
        /// </summary>
        public static char[] OpChars = new char[] { '+', '%', '@', '&', '~' };

        /// <summary>
        /// Try to parse user information out of a full hostmask.
        /// </summary>
        /// <param name="hostmask">The host mask to parse information from.</param>
        /// <returns>A formatted Irc User object with the parsed information.</returns>
        public static Base.User Parse(String hostmask) {
            Match userMatch = RegularExpressions.USER_REGEX.Match(hostmask);

            Base.User user = new Base.User();
            if (userMatch.Success) {
                if (userMatch.Groups["username"].Value != "") {
                    user.DisplayName = userMatch.Groups["nickname"].Value;
                    user.Username = userMatch.Groups["username"].Value;
                } else {
                    user.DisplayName = "<server>";
                    user.Username = "<server>";
                }
            } else {
                user.DisplayName = "<unknown>";
                user.Username = "<unknown>";
            }

            return user;
        }
    }
}

