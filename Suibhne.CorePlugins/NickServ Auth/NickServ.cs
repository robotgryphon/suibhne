using System;
using Ostenvighx.Suibhne.Core;
using Ostenvighx.Suibhne.Plugins;
using Ostenvighx.Api.Networking.Irc;

using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Ostenvighx.Suibhne.CorePlugins {

	public class NickServ : PluginBase {

		public Dictionary<String, NickServConfig> Configurations;

		public NickServ() {
			this.Author = "Ted Senft";
			this.Name = "NickServ Auth Plugin";
			this.Version = "1.0.0";

			this.Configurations = new Dictionary<string, NickServConfig>();
		}

		public virtual void LoadConfiguration(BotServerConnection server){
			String pluginConfigFile = Bot.Configuration.ConfigDirectory + server.Configuration.ConfigurationDirectory + "Plugins/" + this.Name + ".json";

			if(File.Exists(pluginConfigFile)) {
				NickServConfig config = (NickServConfig) NickServConfig.LoadFromFile(pluginConfigFile);

				// Should check config file version and syntax first, but oh well.
				// TODO: Create config file verifier.
				Configurations.Add(server.Configuration.FriendlyName, config);

			} else {
				// Generate new config file
				NickServConfig.GenerateNew(Bot.Configuration.ConfigDirectory + server.Configuration.ConfigurationDirectory + "/Plugins/", this);
			}
		}

		public override void EnableOnServer(BotServerConnection server) {
			LoadConfiguration(server);
			server.OnConnectionComplete += HandleOnConnectionComplete;
		}

		public override void DisableOnServer(BotServerConnection server){
			server.OnConnectionComplete -= HandleOnConnectionComplete;
		}

		void HandleOnConnectionComplete (BotServerConnection server, EventArgs args)
		{
		
			Console.WriteLine("[NickServ Plugin] Identifying with nickserv..");
			// Gather config information out of file
			NickServConfig config = (NickServConfig) Configurations[server.Configuration.FriendlyName];

			server.Connection.SendMessage(config.NickservName, "identify " + config.Password);
		}


	}
}

