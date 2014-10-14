using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

using Ostenvighx.Api.Irc;

namespace Ostenvighx.Suibhne.Core {
	public class IrcBotConfiguration {

		public String ConfigDirectory;
		public List<String> Servers;

		public IrcBotConfiguration(){
			this.ConfigDirectory = Environment.CurrentDirectory + "/Configuration/";
			this.Servers = new List<String>();
		}

		public static IrcBotConfiguration LoadFromFile(String filename) {
			IrcBotConfiguration config = new IrcBotConfiguration();

			// Parse JSON config file
			using (StreamReader file = File.OpenText(filename)) {
				JsonSerializer serializer = new JsonSerializer();
				config = (IrcBotConfiguration) serializer.Deserialize(file, typeof(IrcBotConfiguration));
			}


			return config;
		}
	}
}

