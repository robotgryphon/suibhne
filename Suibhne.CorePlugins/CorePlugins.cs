using System;
using Ostenvighx.Suibhne.Plugins;

namespace Ostenvighx.Suibhne.CorePlugins {
	public class CorePlugins : PluginMain {
		public CorePlugins() {
			this.PluginSetName = "Core Plugins";

			this.AvailablePlugins.Add(typeof(BasicCommands));
			this.AvailablePlugins.Add(typeof(NickServ));
		}
	}
}

