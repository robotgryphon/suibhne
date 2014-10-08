using System;
using Ostenvighx.Suibhne.Configuration;


public class BasicCommandsConfig : PluginConfig {

	public Boolean MessageRequiresOp;

	public BasicCommandsConfig() : base() {
		this.MessageRequiresOp = false;
	}
}