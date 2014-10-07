using System;
using Ostenvighx.Api.Networking.Irc;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

namespace Ostenvighx.Suibhne {
	public class IrcBotConfiguration {

		public String ConfigDirectory;
		public List<String> Servers;

		public IrcBotConfiguration(){
			this.ConfigDirectory = Environment.CurrentDirectory + "/Configuration/";
			this.Servers = new List<String>();
		}

		public static IrcBotConfiguration CreateFromFile(String filename){
			IrcBotConfiguration config = new IrcBotConfiguration();

			using (StreamReader file = File.OpenText(filename)) {
				JsonSerializer serializer = new JsonSerializer();
				config = (IrcBotConfiguration) serializer.Deserialize(file, typeof(IrcBotConfiguration));
			}

			return config;
		}
	}
}

