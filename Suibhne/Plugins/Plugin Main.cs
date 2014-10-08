using System;
using System.Collections.Generic;
using Ostenvighx.Suibhne.Plugins;

namespace Ostenvighx.Suibhne {
	public class PluginMain {

		/// <summary>
		/// Mostly for reference.
		/// </summary>
		public String PluginSetName;

		/// <summary>
		/// Used by plugin registry to create a reference to these plugin files.
		/// </summary>
		public List<Type> AvailablePlugins;

		/// <summary>
		/// Used by Plugin Registry to get a reference to the already-active plugins.
		/// </summary>
		public Dictionary<String, PluginBase> ActivatedPlugins;

		public PluginMain() {
			this.PluginSetName = "Plugin Set";
			this.AvailablePlugins = new List<Type>();
			this.ActivatedPlugins = new Dictionary<String, PluginBase>();
		}
	}
}

