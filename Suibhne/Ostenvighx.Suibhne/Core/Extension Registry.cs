using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Nini.Config;

using Ostenvighx.Suibhne.Core;

namespace Ostenvighx.Suibhne.Extensions {

  public class ExtensionRegistry {

	protected IrcBot bot;

	/// <summary>
	/// An array of all the prepared extension suites.
	/// </summary>
	protected ExtensionSuite[] PreparedSuites;

	public ExtensionRegistry(IrcBot bot) {
	  this.bot = bot;
	  this.PreparedSuites = new ExtensionSuite[0];

	  Initialize();
	}

	public void Initialize() {

	  String[] ExtensionDirectories = Directory.GetDirectories(bot.Configuration.ConfigDirectory + "Extensions/");

	  foreach (String ExtensionsDirectory in ExtensionDirectories) {

		try {
		  String suiteName = ExtensionsDirectory.Substring(ExtensionsDirectory.LastIndexOf("/") + 1);
		  String suiteConf = ExtensionsDirectory + "/Suite.ini";

		  if (File.Exists(suiteConf)) {
			IniConfigSource config = new IniConfigSource(suiteConf);

		  }
		} catch (Exception e) {
		  Console.WriteLine(e);
		}
	  }


	  Console.WriteLine("[Extension System] Loaded " + ExtensionDirectories.Length + " plugins.");
	}


  }
}

