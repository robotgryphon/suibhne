using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Nini.Config;

using Ostenvighx.Suibhne.Core;
using System.Diagnostics;
using System.Threading;

namespace Ostenvighx.Suibhne.Extensions {

  public class ExtensionRegistry {

	protected IrcBot bot;

	/// <summary>
	/// An array of all the prepared extension suites.
	/// </summary>
	protected ExtensionSuite[] PreparedSuites;

	protected Thread extensionThread;

	public ExtensionRegistry(IrcBot bot) {
	  this.bot = bot;
	  this.PreparedSuites = new ExtensionSuite[0];

	  Initialize();
	}

	public void Initialize() {

	  extensionThread = new Thread(new ThreadStart(RunExtensionsServer));
	  extensionThread.Start();

	  String[] ExtensionDirectories = Directory.GetDirectories(bot.Configuration.ConfigDirectory + "Extensions/");

	  foreach (String ExtensionsDirectory in ExtensionDirectories) {

		try {
		  String suiteName = ExtensionsDirectory.Substring(ExtensionsDirectory.LastIndexOf("/") + 1);
		  String suiteConf = ExtensionsDirectory + "/Suite.ini";

		  if (File.Exists(suiteConf)) {
			IniConfigSource config = new IniConfigSource(suiteConf);
			String exec = config.Configs["ExtensionSuite"].GetString("MainExecutable").Trim();
			if(File.Exists(ExtensionsDirectory + "/" + exec)){
			  Process.Start(ExtensionsDirectory + "/" + exec, "6700 " + ((byte) Extensions.RequestCode.Activation));
			}
		  }
		} catch (Exception e) {
		  Console.WriteLine(e);
		}
	  }


	  Console.WriteLine("[Extension System] Loaded " + ExtensionDirectories.Length + " plugins.");
	}

	protected void RunExtensionsServer(){
	  Console.WriteLine("[Extension System] Starting socket for responses...");
	}

  }
}

