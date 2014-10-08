
using System;
using Ostenvighx.Suibhne.Core;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Ostenvighx.Suibhne.Plugins {

	public class PluginRegistry {
	
		public IrcBot bot { get; protected set; }

		public String PluginDirectory { get; protected set; }

		protected Dictionary<String, PluginMain> ActivePluginSets;

		public Dictionary<String, List<PluginBase>> EnabledPlugins { get; protected set; }

		public PluginRegistry(IrcBot bot, String pluginDirectory)
		{
			this.bot = bot;
			this.PluginDirectory = pluginDirectory;
			this.ActivePluginSets = new Dictionary<string, PluginMain>();
			this.EnabledPlugins = new Dictionary<string, List<PluginBase>>();
		}

		public void LoadPluginSet(String filename){
			if(!ActivePluginSets.ContainsKey(filename)){
				String pluginFile = PluginDirectory + filename + ".dll";
				if(File.Exists(pluginFile)) {

					try {
						Assembly LoadedPluginSet = Assembly.LoadFile(pluginFile);

						Type[] types = LoadedPluginSet.GetTypes();
						foreach(Type type in types) {

							if(type.IsSubclassOf(typeof(PluginMain))) {
								PluginMain pluginMain = (PluginMain) Activator.CreateInstance(type);
								ActivePluginSets.Add(filename, pluginMain);

								Console.WriteLine("[Plugins System] Plugin set loaded: " + pluginMain.ToString());

								if(pluginMain.AvailablePlugins.Count > 0){
									// Has available plugins to load.
									foreach(Type pluginType in pluginMain.AvailablePlugins){

										if(pluginType.IsSubclassOf(typeof(PluginBase))){
											PluginBase plugin = (PluginBase) Activator.CreateInstance(pluginType);

											plugin.PrepareBot(bot);
											String pluginClassName = plugin.GetType().ToString().Substring(plugin.GetType().ToString().LastIndexOf(".") + 1);
											Console.WriteLine(pluginClassName);

											pluginMain.ActivatedPlugins.Add(pluginClassName, plugin);

											Console.WriteLine("[Plugins System] Plugin loaded: " + plugin.Name + " (Version: " + plugin.Version + ")");
										} else {
											Console.WriteLine("[Plugins System] Error: Given type is not a plugin or is in an invalid format. [Not subtype of PluginBase]");
										}
									}
								}

							}
						}


					} catch(Exception e){
						Console.WriteLine("[Plugins System] Failed to load plugin set: " + filename);
						Console.WriteLine(e);
					}

				} else {
					// Plugin not available
					throw new FileNotFoundException("Plugin file not found: " + pluginFile);
				}
			}
		}

		public void RegisterPluginSets(List<PluginSet> Plugins){

			foreach(PluginSet pluginSet in Plugins) {
				if(ActivePluginSets.ContainsKey(pluginSet.PluginFile)){
					// Plugin already loaded.
				} else {
					LoadPluginSet(pluginSet.PluginFile);
				}
			}
		}

		// TODO: Change to be able to reload plugins
		// public void EnableOnServer(BotServerConnection server, PluginBase plugin){ }
		public PluginBase GetPlugin(String pluginFile, String pluginName){
			if(ActivePluginSets.ContainsKey(pluginFile)) {
				// Plugin set is loaded, get plugin
				PluginMain pluginMain = ActivePluginSets[pluginFile];

				if(pluginMain.ActivatedPlugins.ContainsKey(pluginName)) {
					return pluginMain.ActivatedPlugins[pluginName];
				} else {
					return null;
				}
			} else {
				LoadPluginSet(pluginFile);
				GetPlugin(pluginFile, pluginName);
			}

			return null;
		}

		public List<PluginBase> GetActivePluginsOnServer(BotServerConnection server){
			if(EnabledPlugins.ContainsKey(server.Configuration.FriendlyName))
				return EnabledPlugins[server.Configuration.FriendlyName];

			return new List<PluginBase>();
		}

		public void EnablePluginsFromList(BotServerConnection server){
		
			if(!EnabledPlugins.ContainsKey(server.Configuration.FriendlyName)) {
				// Add server to plugin tracker
				EnabledPlugins.Add(server.Configuration.FriendlyName, new List<PluginBase>());
			}

			foreach(PluginSet set in server.Configuration.Plugins) {

				foreach(String pluginName in set.Plugins) {

					// Get plugin by name
					PluginBase plugin = GetPlugin(set.PluginFile, pluginName);

					if(plugin != null) {
						Console.WriteLine("[Plugins System] Enabling " + plugin.Name + " on server " + server.Configuration.FriendlyName);
						EnabledPlugins[server.Configuration.FriendlyName].Add(plugin);
						plugin.EnableOnServer(server);
					} else {
						Console.WriteLine("[Plugins System] Failed to enable plugin (" + pluginName + "): Not found");
					}
				}
			}
		}
	}
}

