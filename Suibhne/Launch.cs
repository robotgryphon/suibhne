using System;
using System.Net;
using System.IO;
using System.Collections.Generic;

using Ostenvighx.Api.Networking;
using Ostenvighx.Api.Networking.Irc;

using Ostenvighx.Suibhne.Core;

namespace Ostenvighx.Suibhne {

	public class Launch {

		public static void Main(String[] args){

			IrcBot bot = new IrcBot("/media/ted/Development/Suibhne/Configuration/Servers/Localhost.xml");

			Console.WriteLine(bot.config.hostname);
			Console.WriteLine(bot.config.port);
			Console.WriteLine(bot.config.realname);
			Console.WriteLine(bot.config.username);
			Console.WriteLine(bot.config.nickname);
			Console.WriteLine(bot.config.password);
			// bot.Connect();


		}


	}
}

