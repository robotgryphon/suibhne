using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ostenvighx.Suibhne.Services.Irc {

    /// <summary>
    /// An Networks.Irc User is a user connected to an IRC Network.
    /// This class contains information on their known nicknames,
    /// host mask, and Username.
    /// </summary>
    public class User : Ostenvighx.Suibhne.Services.Chat.User {

        /// <summary>
        /// An array containing all valid operator prefixes.
        /// </summary>
        public static char[] OpChars = new char[] { 
            '+',    // 110
            '%',    // 120
            '@',    // 130
            '&',    // 140
            '~'     // 150
        };

        public User()
            : base() { }

        public User(string username, string last_displayname, string current_displayname) 
            : base(username, last_displayname, current_displayname) { }

        /// <summary>
        /// Try to parse user information out of a full hostmask.
        /// </summary>
        /// <param name="hostmask">The host mask to parse information from.</param>
        /// <returns>A formatted Irc User object with the parsed information.</returns>
        public static User Parse(String hostmask) {
            Match userMatch = RegularExpressions.USER_REGEX.Match(hostmask);

            User user = new User();
            if (userMatch.Success) {
                if (userMatch.Groups["username"].Value != "") {
                    user.DisplayName = userMatch.Groups["nickname"].Value;
                    user.UniqueID = userMatch.Groups["username"].Value + "@" + userMatch.Groups["hostname"].Value;
                } else {
                    user.DisplayName = "<server>";
                    user.UniqueID = "<server>";
                }
            } else {
                user.DisplayName = "<unknown>";
                user.UniqueID = "<unknown>";
            }

            return user;
        }

        public static byte GetAccessLevel(String modes) {
            byte level = 0;
            foreach(char modeChar in modes.ToCharArray()){
                if(modeChar == 'r' && level < 100) level = 100;
                
                if(OpChars.Contains<char>(modeChar))
                    switch(modeChar){
                        case '+':
                            if(level < 110) level = 110;
                            break;

                        case '%':
                            if(level < 120) level = 120;
                            break;

                        case '@':
                            if(level < 130) level = 130;
                            break;

                        case '&':
                            if(level < 140) level = 140;
                            break;

                        case '~':
                            if(level < 150) level = 150;
                            break;
                    }   
            }
            
            return level;
        }
    }
}

