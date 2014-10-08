using System;
using System.Collections.Generic;

namespace Ostenvighx.Suibhne.Plugins {
	public class PluginSet {

		/// <summary>
		/// In the config folder, specifies which file to load the plugins from.
		/// In the plugin code, this names the base assembly (namespace) of the plugin.
		/// </summary>
		public String PluginFile;

		/// <summary>
		/// In the config folder, specifies which plugins should be enabled.
		/// In the plugin code, this specifies all the available plugins in the set.
		/// </summary>
		public List<String> Plugins;

		public PluginSet(){
			this.PluginFile = "";
			this.Plugins = new List<string>();
		}
	}
}