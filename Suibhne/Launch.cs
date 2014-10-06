using System;
using System.Net;
using System.IO;
using System.Collections.Generic;

using Ostenvighx.Api.Networking;
using Ostenvighx.Api.Networking.Irc;

using Ostenvighx.Suibhne.Core;

namespace Ostenvighx.Suibhne {

	public class Launch {

		public static void Main(String[] args) {

			IrcConfig connection = new IrcConfig("irc.furnet.org", 6667, "Suibhne", "Suibhne", "Suibhne", "Suibhne");

			IrcBot bot = new IrcBot(connection);

			// Add delenas to the bot operator list (will soon change to connection op list)
			bot.Operators.Add("delenas");

			// Add #ostenvighx to the autojoin list
			bot.Autojoin.Add("#ostenvighx");

			bot.Connect();

			bot.conn.OnUserJoin += (conn, channel, user, eargs) => {
				conn.SendMessage(channel.channelName, "Hallo, " + user.nickname + "!");
			};

			// Keep the bot alive until bot disconnects
			while(bot.conn.Connected) { }
		}
	}
}

