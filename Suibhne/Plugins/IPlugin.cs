using System;
using Ostenvighx.Api.Networking.Irc;
using Ostenvighx.Suibhne.Core;

namespace Ostenvighx.Suibhne.Plugins {

	public interface IPlugin {

		void Prepare(IrcBot bot);

	}
}

