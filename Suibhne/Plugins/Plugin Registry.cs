
using System;
using Ostenvighx.Suibhne.Core;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Ostenvighx.Suibhne.Plugins {

	public class PluginRegistry {
	
		public IrcBot bot { get; protected set; }

		public String PluginDirectory { get; protected set; }

		protected Dictionary<String, PluginBase> ActivePlugins;
		protected List<String> ActivePluginSets;

		public PluginRegistry(IrcBot bot, String pluginDirectory)
		{
			this.bot = bot;
			this.PluginDirectory = pluginDirectory;
			this.ActivePlugins = new Dictionary<String, PluginBase>();
			this.ActivePluginSets = new List<string>();
		}

		public void LoadPluginSet(String filename){
			if(!ActivePluginSets.Contains(filename)){
				String pluginFile = PluginDirectory + filename + ".dll";
				if(File.Exists(pluginFile)) {
				
					Assembly LoadedPlugin = Assembly.LoadFile(pluginFile);

					Type[] types = LoadedPlugin.GetTypes();
					foreach(Type type in types) {

						if(type.IsSubclassOf(typeof(PluginBase))) {
							// Plugin found

							PluginBase plugin = (PluginBase)Activator.CreateInstance(type);

							plugin.Prepare(this.bot);

							// Add plugin to loaded list
							String pluginName = plugin.GetType().ToString().Substring(plugin.GetType().ToString().LastIndexOf(".") + 1);

							if(!ActivePlugins.ContainsKey(pluginName))
								ActivePlugins.Add(pluginName, plugin);
						}
					}
				} else {
					// Plugin not available
				}
			}
		}

		public void RegisterPlugins(List<PluginContainer> Plugins){

			foreach(PluginContainer plugin in Plugins) {
				if(ActivePluginSets.Contains(plugin.PluginFile)){
					// Plugin already loaded.
				} else {
					LoadPluginSet(plugin.PluginFile);
				}
			}
		}

		public void EnablePlugins(BotServerConnection server){
			foreach(PluginContainer plugin in server.Configuration.Plugins) {
				foreach(String pluginName in plugin.EnabledPlugins) {
					if(ActivePlugins.ContainsKey(pluginName)) {
						ActivePlugins[pluginName].EnableOnServer(server);
					}
				}
			}
		}
	}
}

