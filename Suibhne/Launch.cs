using System;
using System.Net;
using System.IO;
using System.Collections.Generic;

using Ostenvighx.Api.Networking;
using Ostenvighx.Api.Networking.Irc;

using Ostenvighx.Suibhne.Core;
using System.Threading;

namespace Ostenvighx.Suibhne {

	public class Launch {

		public static void Main(String[] args) {

			IrcConfig connection = IrcConfig.LoadFromFile(Environment.CurrentDirectory + "/Configuration/Servers/localhost/Server.json");
			Console.WriteLine(connection.configDir);


			IrcBot bot = new IrcBot(connection);

			// Add delenas to the bot operator list (will soon change to connection op list)
			bot.Operators.Add("delenas");

			// Add #ostenvighx to the autojoin list
			bot.Autojoin.Add("#ostenvighx");

			bot.Connect();

			// Keep the bot alive until bot disconnects
			while(bot.conn.Connected) { }

		}
	}
}

