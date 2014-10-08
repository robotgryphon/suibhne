using System;
using Ostenvighx.Suibhne.Core;
using Ostenvighx.Suibhne.Plugins;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Ostenvighx.Suibhne.Commands {

	/// <summary>
	/// The command registry manages the commands for plugins on a specified Server.
	/// </summary>
	public class CommandRegistry {

		public Dictionary<String, IrcBotCommand> RegisteredCommands;

		public CommandRegistry() {
		}

		// DoesCommandExist
		// GetCommandByString
	}
}

