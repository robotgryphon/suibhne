using System;
using System.IO;
using Newtonsoft.Json;
using Ostenvighx.Suibhne.Plugins;

namespace Ostenvighx.Suibhne.CorePlugins {
	/// <summary>
	/// Holds the nickserv configuration layout.
	/// </summary>
	public class NickServConfig {
		public String NickservName;
		public String Password;

		public static NickServConfig LoadFromFile(String filename) {
			NickServConfig config = new NickServConfig();

			// Parse JSON config file
			using (StreamReader file = File.OpenText(filename)) {
				JsonSerializer serializer = new JsonSerializer();
				config = (NickServConfig) serializer.Deserialize(file, typeof(NickServConfig));
			}


			return config;
		}

		public static void GenerateNew(String pluginDirectory, PluginBase plugin){
			// TODO: Generate
		}
	}


}
