
using System;
using Ostenvighx.Suibhne.Core;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Ostenvighx.Suibhne.Plugins {

	public class PluginRegistry {

		protected IrcBot bot;
		protected List<PluginBase> Plugins;

		public PluginRegistry(IrcBot bot)
		{
			this.bot = bot;
			this.Plugins = new List<PluginBase>();
		}

		public void RegisterPlugins(){

			Type pluginType = typeof(PluginBase);

			String[] plugins = Directory.GetFiles(Environment.CurrentDirectory + "/Configuration/Plugins/", "*.dll");
			foreach(String pluginFile in plugins) {
				Console.WriteLine("Plugin dll found: " + pluginFile);
				Assembly LoadedPlugin = Assembly.LoadFile(pluginFile);

				Type[] types = LoadedPlugin.GetTypes();
				foreach(Type type in types) {

					if(type.IsSubclassOf(pluginType)) {
						// Plugin found

						PluginBase plugin = (PluginBase) Activator.CreateInstance(type);
						Console.WriteLine("Plugin Loaded: " + plugin.Name);

						plugin.Prepare(bot);

						// Add plugin to loaded list
						Plugins.Add(plugin);
					}
				}
			}
		}
	}
}

