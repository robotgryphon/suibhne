using System;
using Nini;
using Nini.Config;

namespace Ostenvighx.Suibhne {

	public class PluginConfiguration {

		String FileName;
		ConfigBase configFile;

		public PluginConfiguration(String filename) {
			this.FileName = filename;

		}
	}

}

