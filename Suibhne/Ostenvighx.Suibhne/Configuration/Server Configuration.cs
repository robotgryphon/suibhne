using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using Nini.Config;

using Ostenvighx.Api.Irc;
using Ostenvighx.Suibhne.Plugins;

namespace Ostenvighx.Suibhne.Core {
	public class ServerConfig {

		public IrcConfig Server;

		public String FriendlyName;

		public String[] Operators;

		public IrcLocation[] AutoJoinChannels;

		public String[] Plugins;

		public static ServerConfig CreateNew(){
			ServerConfig config = new ServerConfig();
			config.Operators = new String[]{ };
			config.Plugins = new String[]{ };
			config.Server = new IrcConfig();
			config.FriendlyName = "Default";

			config.AutoJoinChannels = new IrcLocation[]{ };

			return config;
		}

		public static ServerConfig LoadFromFile(String filename) {
			char[] delim = new char[]{','};

			ServerConfig config = new ServerConfig();

			IConfigSource conf = new IniConfigSource(filename);

			Regex serverName = new Regex(@"[\/\\](?<fname>\w+).ini");

			config.FriendlyName = serverName.Match(filename).Groups["fname"].Value;
			String[] plugs = conf.Configs["Plugins"].GetString("enabled").Split(delim, StringSplitOptions.RemoveEmptyEntries);
			List<String> plugins = new List<string>();
			foreach(String p in plugs)
				plugins.Add(p.Trim());

			config.Plugins = plugins.ToArray();

			String[] ops = conf.Configs["Operators"].GetString("serverwide").Split(delim, StringSplitOptions.RemoveEmptyEntries);
			List<String> opsList = new List<string>();
			foreach(String op in ops)
				opsList.Add(op.Trim());

			config.Operators = opsList.ToArray();

			string[] chanlist = conf.Configs["Channels"].GetString("autojoin").Split(delim, StringSplitOptions.RemoveEmptyEntries);
			List<IrcLocation> chans = new List<IrcLocation>();
			foreach(string chan in chanlist)
				chans.Add(new IrcLocation(chan.Trim()));

			config.AutoJoinChannels = chans.ToArray();

			config.Server = IrcConfig.LoadFromFile(filename);

			return config;
		}
	}
}

