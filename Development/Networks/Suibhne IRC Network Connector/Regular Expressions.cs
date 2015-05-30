using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Ostenvighx.Suibhne.Networks.Irc {

    /// <summary>
    /// This class holds static Regular Expressions and strings for matching IRC-related
    /// strings of text.
    /// </summary>
    public struct RegularExpressions {

        /// <summary>
        /// Part of a regex used to match all valid IRC nicknames.
        /// </summary>
        public static String NICKNAME_REGEX_RAW = @"[^!]+";

        /// <summary>
        /// A quick way to access a valid userhost regex.
        /// This is in the format of DisplayName![~]Username@userhost.
        /// Fields are usernick, Username, and userhost. For full match, use userfull.
        /// </summary>
        public static String SENDER_REGEX_RAW = @"(?<sender>(?<nickname>" + NICKNAME_REGEX_RAW + @")(?<username>(?:!)([^@]*))?(?<hostname>[\w.:]+)?)";

        /// <summary>
        /// Matches a given origin location on a server. This is a locationID, Username, etc.
        /// </summary>
        public static String LOCATION_REGEX = @"(?<location>\#?.+)";

        /// <summary>
        /// Quick wrap for Username host parsing.
        /// See USERHOST_REGEX for details.
        /// </summary>
        public static Regex USER_REGEX = new Regex(SENDER_REGEX_RAW);

        /// <summary>
        /// Matches a names response for locationID user lists.
        /// </summary>
        public static Regex NAMES_RESPONSE = new Regex(@"(?<servhost>[\w\d\.\:]+)\s(?:[\d]+)\s(?:\w+)\s.\s(?<nicks>[\#\%\&][\w]+)\s\:(.*)+");

        /// <summary>
        /// Matches a WHOIS response (code 311)
        /// </summary>
        public static Regex WHOIS_USER = new Regex(SENDER_REGEX_RAW + @"\s311\s(?:" + NICKNAME_REGEX_RAW +
            @")\s(?<nick>" + NICKNAME_REGEX_RAW +
            @")\s(?<user>[\w\d\-]+)\s" +
            @"(?<userhost>[\.\w\d]+)\s\*\s\:(?<realname>.*)");
    }
}
