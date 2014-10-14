﻿using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

using Ostenvighx.Api.Irc;
using Ostenvighx.Suibhne.Plugins;

namespace Ostenvighx.Suibhne.Core {
	public class ServerConfig {

		public IrcConfig Server;

		public String FriendlyName;

		public List<String> Operators;

		public List<IrcChannel> AutoJoinChannels;

		public List<String> Plugins;

		public String ConfigurationDirectory;

		public static ServerConfig CreateNew(){
			ServerConfig config = new ServerConfig();
			config.Operators = new List<String>();
			config.Plugins = new List<String>();
			config.Server = new IrcConfig();
			config.FriendlyName = "Default";

			config.AutoJoinChannels = new List<IrcChannel>();

			config.ConfigurationDirectory = "Servers/Default/";
			return config;
		}

		public static ServerConfig LoadFromFile(String filename) {
			ServerConfig config = new ServerConfig();

			// Parse JSON config file
			using (StreamReader file = File.OpenText(filename)) {
				JsonSerializer serializer = new JsonSerializer();
				config = (ServerConfig) serializer.Deserialize(file, typeof(ServerConfig));
			}


			return config;
		}
	}
}
