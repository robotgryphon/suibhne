using System;
using System.Net;
using System.IO;
using System.Collections.Generic;

using Ostenvighx.Api.Networking;
using Ostenvighx.Api.Networking.Irc;

using Ostenvighx.Suibhne.Core;
using System.Threading;
using Newtonsoft.Json;

namespace Ostenvighx.Suibhne {

	public class Launch {

		public static void Main(String[] args) {

			IrcBot bot = new IrcBot();
			Console.WriteLine(bot.Configuration.ConfigDirectory);

			ServerConfig localhost = ServerConfig.LoadFromFile(bot.Configuration.ConfigDirectory + "Servers/Localhost/Server.json");
			ServerConfig furnet = ServerConfig.LoadFromFile(bot.Configuration.ConfigDirectory + "Servers/Furnet/Server.json");

			BotServerConnection localhostServer = new BotServerConnection(localhost, bot.Plugins);
			BotServerConnection furnetServer = new BotServerConnection(furnet, bot.Plugins);

			localhostServer.Connect();
			furnetServer.Connect();

			while(true) {
			}
		}
	}
}

