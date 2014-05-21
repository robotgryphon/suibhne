
using System;
using Ostenvighx.Suibhne;

namespace Ostenvighx.Suibhne.Modules {

	/// <summary>
	/// Example of an IRC bot module for Suibhne 2.0.
	/// </summary>
	public class ExampleIrcBotModule : IrcBotModule {

		/// <summary>
		/// Create a new IrcBotModule instance.
		/// </summary>
		/// <param name="moduleName">Module name.</param>
		public ExampleIrcBotModule( String moduleName )
			:base(moduleName)
		{

		}
	}
}

