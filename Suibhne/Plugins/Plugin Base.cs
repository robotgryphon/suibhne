
using System;
using Ostenvighx.Suibhne.Core;
using System.Collections.Generic;
using Ostenvighx.Suibhne.Configuration;
using System.IO;

namespace Ostenvighx.Suibhne.Plugins {

	/// <summary>
	/// An IRC Bot Module contains code that is linked in with an IRC server.
	/// Things happen and events are fired off when certain conditions in the module's
	/// listeners and update methods occur. This is useful for plugin-style code management,
	/// for things such as custom Authentication servers, media plugins, polls, etc...
	/// </summary>
	public abstract class PluginBase {

		public IrcBot Bot { get; protected set; }

		/// <summary>
		/// The friendly name for the plugin.
		/// </summary>
		public String Name { get; protected set; }

		/// <summary>
		/// This is a custom identifier for the module to use.
		/// </summary>
		public Guid Identifier { get; protected set; }

		/// <summary>
		/// Author of the plugin.
		/// </summary>
		public String Author { get; protected set; }

		/// <summary>
		/// Current version of the plugin.
		/// May be used for version update checking in the future.
		/// </summary>
		public String Version { get; protected set; }

		/// <summary>
		/// Holds the loaded server configurations. Also is a list of all the servers this plugin is loaded on (by key)
		/// </summary>
		public Dictionary<String, PluginConfig> Configurations;

		/// <summary>
		/// Create a new IrcBotModule instance.
		/// </summary>
		/// <param name="moduleName">Module name.</param>
		public PluginBase()
		{
			this.Name = "Plugin";
			this.Identifier = Guid.NewGuid();
			this.Author = "Plugin Author";
			this.Version = "0.0.1";

			this.Configurations = new Dictionary<string, PluginConfig>();
		}

		public virtual void PrepareBot(IrcBot bot){
			this.Bot = bot;
		}

		/// <summary>
		/// Called to load the configuration file for the plugin.
		/// </summary>
		/// <param name="server">Server to load configuration file for.</param>
		public virtual void LoadConfiguration(BotServerConnection server){
			String pluginConfigFile = Bot.Configuration.ConfigDirectory + server.Configuration.ConfigurationDirectory + "Plugins/" + this.Name + ".json";

			if(File.Exists(pluginConfigFile)) {
				PluginConfig config = (PluginConfig) PluginConfig.LoadFromFile(pluginConfigFile);

				// Should check config file version and syntax first, but oh well.
				// TODO: Create config file verifier.
				Configurations.Add(server.Configuration.FriendlyName, config);

			} else {
				// Generate new config file
			}
		}

		public void RefreshConfiguration(BotServerConnection server){
			this.Configurations.Remove(server.Configuration.FriendlyName);
			LoadConfiguration(server);
		}

		/// <summary>
		/// Prepare the plugin by hooking into all the necessary events this plugin will need.
		/// </summary>
		public abstract void EnableOnServer(BotServerConnection server);

		/// <summary>
		/// Disable the plugin by unhooking all the events previously hooked.
		/// </summary>
		public abstract void DisableOnServer(BotServerConnection server);
	}
}

