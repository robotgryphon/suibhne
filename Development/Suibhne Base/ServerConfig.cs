using Raindrop.Api.Irc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Raindrop.Suibhne {
    public struct ServerConfig {

        public String Hostname;
        public int Port;
        public String ServPassword;

        public String Nickname;
        public String Username;
        public String DisplayName;
        public String AuthPassword;

        public IrcLocation[] AutoJoinChannels;
    }
}
