using System;
using Ostenvighx.Suibhne.Core;
using Ostenvighx.Api.Networking.Irc;

namespace Suibhne.Core {

	public struct IrcBotCommand {

		public IrcBot requester;
		public String command;
		public IrcMessage message;

	}
}

