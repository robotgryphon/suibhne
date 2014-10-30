using System;
using System.Collections.Generic;
using System.IO;

using Nini.Config;

using Ostenvighx.Api.Irc;

namespace Ostenvighx.Suibhne.Core {
	public class IrcBotConfiguration {

		public String ConfigDirectory;
		public String[] Servers;

		public IrcBotConfiguration(){
			this.ConfigDirectory = Environment.CurrentDirectory + "/Configuration/";
			this.Servers = new String[]{ };
		}

		public static IrcBotConfiguration LoadFromFile(String filename) {
			IrcBotConfiguration config = new IrcBotConfiguration();
			IConfigSource conf = new IniConfigSource(filename);

			config.ConfigDirectory = conf.Configs["Suibhne"].GetString("configDir", Environment.CurrentDirectory + "/Configuration/");

			string[] servs = conf.Configs["Suibhne"].GetString("servers").Split(new char[]{ ',' }, StringSplitOptions.RemoveEmptyEntries);
			List<String> serverList = new List<string>();
			foreach(String s in servs)
				serverList.Add(s.Trim());

			config.Servers = serverList.ToArray();

			return config;
		}
	}
}

