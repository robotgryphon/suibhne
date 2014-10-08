using System;
using Ostenvighx.Suibhne.Plugins;
using Ostenvighx.Api.Networking.Irc;

namespace Ostenvighx.Suibhne.CorePlugins {
	public class Greeter : PluginBase {
		public Greeter() : base() {
			this.Author = "Ted Senft";
			this.Version = "0.0.1";
			this.Name = "AutoGreeter";
		}

		public override void EnableOnServer(Ostenvighx.Suibhne.Core.BotServerConnection server) {
			server.Connection.OnUserJoin += HandleOnUserJoin;
		}

		public override void DisableOnServer(Ostenvighx.Suibhne.Core.BotServerConnection server) {
			server.Connection.OnUserPart += HandleOnUserJoin;
		}

		void HandleOnUserJoin (IrcConnection conn, IrcChannel channel, IrcUser user)
		{
			if(user.nickname.ToLower() != conn.CurrentNickname.ToLower()) {
				IrcMessage greeting = new IrcMessage(channel.channelName, conn.CurrentNickname, "waves to " + user.nickname + ". \"Hello, there!\"");
				greeting.isAction = true;
	
				conn.SendMessage(greeting);
			}
		}
	}
}

