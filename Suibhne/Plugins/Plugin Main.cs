using System;
using System.Collections.Generic;
using Ostenvighx.Suibhne.Plugins;

namespace Ostenvighx.Suibhne {
	public class PluginMain {

		public List<Type> AvailablePlugins;
		public List<PluginBase> ActivatedPlugins;

		public PluginMain() {
			this.AvailablePlugins = new List<Type>();
			this.ActivatedPlugins = new List<PluginBase>();
		}
	}
}

