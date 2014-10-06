
using System;
using Ostenvighx.Suibhne.Core;

namespace Ostenvighx.Suibhne.Plugins {

	/// <summary>
	/// An IRC Bot Module contains code that is linked in with an IRC server.
	/// Things happen and events are fired off when certain conditions in the module's
	/// listeners and update methods occur. This is useful for plugin-style code management,
	/// for things such as custom Authentication servers, media plugins, polls, etc...
	/// </summary>
	public abstract class PluginBase {

		public IrcBot bot { get; protected set; }

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
		/// Create a new IrcBotModule instance.
		/// </summary>
		/// <param name="moduleName">Module name.</param>
		public PluginBase()
		{
			this.Name = "Plugin";
			this.Identifier = Guid.NewGuid();
			this.Author = "Plugin Author";
			this.Version = "0.0.1";
		}


		public virtual void Prepare(IrcBot bot){
			this.bot = bot;
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

