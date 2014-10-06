using System;
using Ostenvighx.Api.Networking.Irc;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;

namespace Ostenvighx.Suibhne {
	public struct IrcBotConfiguration {

		public String ConfigDirectory;
		public String[] Servers;

		public void LoadFrom(String filename){
			using (StreamReader file = File.OpenText(filename)) {
				JsonSerializer serializer = new JsonSerializer();
				this = (IrcBotConfiguration) serializer.Deserialize(file, typeof(IrcBotConfiguration));
			}
		}
	}
}

