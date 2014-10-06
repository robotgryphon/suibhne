using System;
using Ostenvighx.Suibhne.Core;
using Ostenvighx.Suibhne.Plugins;
using Ostenvighx.Api.Networking.Irc;

namespace Suibhne.CorePlugins {
	public class NickServ : PluginBase {

		public NickServ() {
			this.Author = "Ted Senft";
			this.Name = "NickServ Auth Plugin";
		}

		public override void Prepare(IrcBot bot) {
			bot.conn.OnConnectionComplete += HandleOnConnectionComplete;;
		}

		void HandleOnConnectionComplete (IrcConnection conn, EventArgs args)
		{
			Console.WriteLine("[NickServ Plugin] Connection started!");
			// Gather config information out of file

			// conn.SendMessage("NickServ", "identify " + PluginConfig.Password);
		}
	}
}

