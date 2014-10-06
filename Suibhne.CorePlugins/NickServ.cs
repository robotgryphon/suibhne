using System;
using Ostenvighx.Suibhne.Core;
using Ostenvighx.Suibhne.Plugins;
using Ostenvighx.Api.Networking.Irc;

using Newtonsoft.Json;

namespace Ostenvighx.Suibhne.CorePlugins {

	/// <summary>
	/// Holds the nickserv configuration layout.
	/// </summary>
	public struct NickServConfig {
		public String NickservName;
		public String Password;
	}

	public class NickServ : PluginBase {

		public NickServ() {
			this.Author = "Ted Senft";
			this.Name = "NickServ Auth Plugin";
			this.Version = "1.0.0";
		}
			
		public override void EnableOnServer(BotServerConnection server) {
			server.OnConnectionComplete += HandleOnConnectionComplete;
		}

		public override void DisableOnServer(BotServerConnection server){
			server.OnConnectionComplete -= HandleOnConnectionComplete;
		}

		void HandleOnConnectionComplete (BotServerConnection server, EventArgs args)
		{
		
			Console.WriteLine("[NickServ Plugin] Identifying with nickserv..");
			// Gather config information out of file

			String filename = bot.Configuration.ConfigDirectory + server.Configuration.ConfigurationDirectory + "/Plugins/NickServ.json";

			String nickservText = System.IO.File.ReadAllText(filename);

			NickServConfig config = JsonConvert.DeserializeObject<NickServConfig>(nickservText);

			server.Connection.SendMessage(config.NickservName, "identify " + config.Password);
			
		}


	}
}

