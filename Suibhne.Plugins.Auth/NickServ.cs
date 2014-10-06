using System;
using Ostenvighx.Suibhne.Core;
using Ostenvighx.Suibhne.Plugins;
using Ostenvighx.Api.Networking.Irc;

namespace Suibhne.Plugins.Auth {
	public class NickServPlugin : PluginBase {

		public NickServPlugin() {
			this.Author = "Ted Senft";
			this.Name = "NickServ Auth Plugin";
		}

		public override void Prepare(IrcBot bot) {
			bot.conn.OnConnectionComplete += HandleOnConnectionComplete;;
		}

		void HandleOnConnectionComplete (IrcConnection conn, EventArgs args)
		{
			Console.WriteLine("[NickServ Plugin] Connection registered! Ready to send passwords and stuffs.");
		}
	}
}

