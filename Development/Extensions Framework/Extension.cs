using System;
using System.Collections.Generic;

using System.IO;

namespace Raindrop.Suibhne.Extensions {

  /// <summary>
  /// An extension class here is used to create useful functions and helpers for extension creation.
	
  /// </summary>
  public abstract class Extension {

	/// <summary>
	/// The friendly name for the extension to refer to itself as.
	/// </summary>
	public String Name { get; protected set; }

	/// <summary>
	/// This is a custom identifier for the module to use.
	/// </summary>
	public Guid Identifier { get; protected set; }


	public Extension() {
	  this.Name = "Plugin";
	  this.Identifier = Guid.NewGuid();
	}
  }
}

