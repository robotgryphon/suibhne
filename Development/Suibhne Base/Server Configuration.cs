using Raindrop.Api.Irc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Nini.Config;

namespace Raindrop.Suibhne {
    public class ServerConfig {

        public String Hostname;
        public int Port;
        public String ServPassword;

        public String Nickname;
        public String Username;
        public String DisplayName;
        public String AuthPassword;

        public IrcLocation[] AutoJoinChannels;

        public String[] Operators;

        public ServerConfig() {
            this.Hostname = "localhost";
            this.Port = 6667;
            this.ServPassword = "";

            this.Nickname = "Suibhne";
            this.Username = "suibhne";
            this.DisplayName = "Suibhne IRC Bot";
            this.AuthPassword = "";

            this.AutoJoinChannels = new IrcLocation[0];
            this.Operators = new string[0];
        }

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

            String[] autojoinChannels = server.GetString("Autojoin", "").Replace(" ", "").Split(new char[] { ',' });
            List<IrcLocation> chans = new List<IrcLocation>();
            foreach (String chan in autojoinChannels) {
                chans.Add(new IrcLocation(chan));
            }

            config.AutoJoinChannels = chans.ToArray();
            config.Operators = server.GetString("Operators", "").Replace(" ", "").Split(new char[] { ',' });

            return config;
        }
    }
}
