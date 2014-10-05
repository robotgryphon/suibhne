
using System;
using Ostenvighx.Suibhne.Core;

namespace Ostenvighx.Suibhne.Plugins {

	public class PluginRegistry {

		protected IrcBot bot;

		public PluginRegistry(IrcBot bot)
		{
			this.bot = bot;
		}
	}
}

