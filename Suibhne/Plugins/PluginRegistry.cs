
using System;
using Ostenvighx.Suibhne.Core;

namespace Suibhne {

	public class PluginRegistry {

		protected IrcBot bot;

		public PluginRegistry(IrcBot bot)
		{
			this.bot = bot;
		}
	}
}

