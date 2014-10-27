
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Ostenvighx.Suibhne.Core;

namespace Ostenvighx.Suibhne.Plugins {

	public class PluginRegistry {
	
		public IrcBot bot { get; protected set; }

		/// <summary>
		/// A list of all the loaded plugins on the bot, referenced by ID. This should
		/// contain ALL of the plugins in the plugins directory.
		/// </summary>
		protected Dictionary<int, PluginBase> LoadedPlugins;

		/// <summary>
		/// A dictionary of plugins enabled on a server. The key is the server's friendly name
		/// and the values is a list of all the active plugin files on said server.
		/// </summary>
		/// <value>The enabled plugins on the servers.</value>
		public Dictionary<String, List<int>> EnabledPlugins { get; protected set; }

		public PluginRegistry(IrcBot bot)
		{
			this.bot = bot;
			this.LoadedPlugins = new Dictionary<int, PluginBase>();
			this.EnabledPlugins = new Dictionary<string, List<int>>();

			InitializePlugins();
		}

		public void InitializePlugins(){

			String[] PluginFolders = Directory.GetDirectories(bot.Configuration.ConfigDirectory + "Plugins/");

			foreach(String PluginFolder in PluginFolders) {
			
				String PluginName = PluginFolder.Substring(PluginFolder.LastIndexOf("/") + 1);
				String pluginFile = PluginFolder + "/" + PluginName + ".dll";

				if(File.Exists(pluginFile)) {

					try {
						Assembly LoadedPlugin = Assembly.LoadFile(pluginFile);

						Type[] types = LoadedPlugin.GetTypes();

						foreach(Type type in types) {

							if(type.IsSubclassOf(typeof(PluginBase))) {
								PluginBase plugin = (PluginBase) Activator.CreateInstance(type);

								plugin.PrepareBot(bot);
								String pluginClassName = type.ToString().Substring(plugin.GetType().ToString().LastIndexOf(".") + 1);

								int newID = LoadedPlugins.Count + 1;
								LoadedPlugins.Add(newID, plugin);

								Console.WriteLine("[Plugins System] Plugin loaded: " + plugin.Name + " (Version: " + plugin.Version + ", Given ID #" + newID + ")");
							}
						}

					} catch(Exception e){
						Console.WriteLine("[Plugins System] Failed to load plugin: " + PluginName);
						Console.WriteLine(e);
					}
				}
			}

			Console.WriteLine("[Plugins System] Loaded " + LoadedPlugins.Count + " plugins.");
		}

		public int GetPluginID(String pluginName){
			foreach(KeyValuePair<int, PluginBase> plugin in LoadedPlugins) {
				if(plugin.Value.Name.ToLower() == pluginName.ToLower()) {
					return plugin.Key;
				}
			}

			return -1;
		}

		public PluginBase GetPlugin(String pluginName){
			int pluginID = GetPluginID(pluginName);
			if(pluginID != -1)
				return GetPlugin(pluginID);

			return null;
		}

		public PluginBase GetPlugin(int pluginID){
			if(LoadedPlugins.ContainsKey(pluginID)) {
				// Plugin set is loaded, get plugin
				return LoadedPlugins[pluginID];
			}

			return null;
		}

		public void EnablePluginOnServer(int pluginID, BotServerConnection server){
			// Do checks to make sure plugin exists
			if(pluginID > 0 && LoadedPlugins.ContainsKey(pluginID)) {
				GetPlugin(pluginID).EnableOnServer(server);
				EnabledPlugins[server.Configuration.FriendlyName].Add(pluginID);
			} else {
				Console.WriteLine("Plugin not enabled. Not found in the loaded plugin registry.");
			}
		}

		public void DisablePluginOnServer(int pluginID, BotServerConnection server){
			// Do checks to make sure plugin exists
			if(pluginID > 0 && LoadedPlugins.ContainsKey(pluginID)) {
				GetPlugin(pluginID).DisableOnServer(server);
				EnabledPlugins[server.Configuration.FriendlyName].Remove(pluginID);
			}
		}

		/// <summary>
		/// Get a list of all the active plugins on a server by their integer IDs.
		/// </summary>
		/// <returns>The active plugins on server, by reference to their loaded PluginBase.</returns>
		/// <param name="server">Server to get plugin list for.</param>
		public int[] GetActivePluginsOnServer(BotServerConnection server){
			if(EnabledPlugins.ContainsKey(server.Configuration.FriendlyName))
				return EnabledPlugins[server.Configuration.FriendlyName].ToArray();
				
			return new int[0];
		}

		public int[] GetUnactivePluginsOnServer(BotServerConnection server){
			int[] AllPlugins = LoadedPlugins.Keys.ToArray();
			return AllPlugins.Except(GetActivePluginsOnServer(server)).ToArray();
		}

		/// <summary>
		/// Quickly enable all the plugins on a server's configured plugin list.
		/// </summary>
		/// <param name="server">Server to enable plugins on/for.</param>
		public void EnablePluginsOnServer(BotServerConnection server){
			if(EnabledPlugins.ContainsKey(server.Configuration.FriendlyName)) {
				List<String> pluginsToEnable = server.Configuration.Plugins;
				foreach(String pluginToEnable in pluginsToEnable) {
					int pluginID = GetPluginID(pluginToEnable);
					EnablePluginOnServer(pluginID, server);
				}
			} else {
				EnabledPlugins.Add(server.Configuration.FriendlyName, new List<int>());
				EnablePluginsOnServer(server);
			}
		}
	}
}

