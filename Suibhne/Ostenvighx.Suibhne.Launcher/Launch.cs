using System;
using Ostenvighx.Suibhne.Core;

namespace Ostenvighx.Suibhne {

	public class Launch {

		public static void Main(String[] args) {

			IrcBot bot = new IrcBot();
			bot.LoadServers();
			bot.Start();

            Console.Title = "Suibhne IRC System";
            Console.Clear();

			while(true) {
				// Keep alive
			}
		}
	}
}

