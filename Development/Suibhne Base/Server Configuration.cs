using Raindrop.Api.Irc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Nini.Config;

namespace Raindrop.Suibhne {
    public struct ServerConfig {

        public String Hostname;
        public int Port;
        public String ServPassword;

        public String Nickname;
        public String Username;
        public String DisplayName;
        public String AuthPassword;

        public Location[] AutoJoinChannels;

        public String[] Operators;

        public static ServerConfig LoadFromFile(String filename) {
            ServerConfig config = new ServerConfig();

            IniConfigSource csource = new IniConfigSource(filename);
            IConfig server = csource.Configs["Server"];
            
            config.Hostname = server.GetString("Hostname", "localhost");
            config.Port = server.GetInt("Port", 6667);
            config.ServPassword = server.GetString("ServPassword", "");

            config.Nickname = server.GetString("Nickname", "Suibhne");
            config.Username = server.GetString("Username", "suibhne");
            config.DisplayName = server.GetString("DisplayName", "");
            config.AuthPassword = server.GetString("AuthPassword", "");

            String[] autojoinChannels = csource.Configs["Locations"].GetString("Autojoin", "").Replace(" ", "").Split(new char[] { ',' });
            List<Location> chans = new List<Location>();
            foreach (String chan in autojoinChannels) {
                chans.Add(new Location(chan));
            }

            config.AutoJoinChannels = chans.ToArray();
            config.Operators = server.GetString("Operators", "").Replace(" ", "").Split(new char[] { ',' });

            return config;
        }
    }
}
