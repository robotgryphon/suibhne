using System;
using System.Collections.Generic;
using Ostenvighx.Suibhne.Plugins;

namespace Ostenvighx.Suibhne {
	public class PluginMain {

		public String PluginSetName;

		public List<Type> AvailablePlugins;
		public Dictionary<String, PluginBase> ActivatedPlugins;

		public PluginMain() {
			this.PluginSetName = "Plugin Set";
			this.AvailablePlugins = new List<Type>();
			this.ActivatedPlugins = new Dictionary<String, PluginBase>();
		}
	}
}

