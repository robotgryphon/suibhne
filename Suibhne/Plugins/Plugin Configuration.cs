using System;
using System.IO;
using Newtonsoft.Json;

namespace Ostenvighx.Suibhne.Configuration {
	public class PluginConfig {

		public PluginConfig(){

		}

		public static PluginConfig LoadFromFile(String filename){

			PluginConfig config = new PluginConfig();

			// Parse JSON config file
			using (StreamReader file = File.OpenText(filename)) {
				JsonSerializer serializer = new JsonSerializer();
				config = (PluginConfig) serializer.Deserialize(file, typeof(PluginConfig));
			}


			return config;

		}
	}
}

