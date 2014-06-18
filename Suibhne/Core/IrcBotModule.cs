
using System;

namespace Ostenvighx.Suibhne.Core {

	/// <summary>
	/// An IRC Bot Module contains code that is linked in with an IRC server.
	/// Things happen and events are fired off when certain conditions in the module's
	/// listeners and update methods occur. This is useful for plugin-style code management,
	/// for things such as custom Authentication servers, media plugins, polls, etc...
	/// </summary>
	public class IrcBotModule {

		/// <summary>
		/// This is a custom identifier for the module to use.
		/// It should be completely unique, and a good practice to follow for this sort
		/// of thing would be to refer to the module by a package-like name, such as
		/// "com.example.suibhne.modules.ExampleModule"
		/// </summary>
		/// <value>The name of the module.</value>
		public String ModuleName { get; protected set; }

		public IrcBot bot;

		/// <summary>
		/// Create a new IrcBotModule instance.
		/// </summary>
		/// <param name="moduleName">Module name.</param>
		public IrcBotModule( String moduleName )
		{
			this.ModuleName = moduleName;
		}
	}
}

