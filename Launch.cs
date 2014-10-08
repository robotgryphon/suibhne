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
			bot.LoadServers();
			bot.Start();

			while(true) {
				// Keep alive
			}
		}
	}
}

