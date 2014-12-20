using System;
using System.Net.Sockets;

namespace Ostenvighx.Suibhne.Extensions {

  public enum RequestCode : byte {
	Unknown = 0,

	ExtensionName = 1,
	ExtensionVersion = 2,
	ExtensionUpdateTime = 3
  }

  /// <summary>
  /// An extension suite holds a filename to an extension executable, the IDs of the extensions in
  /// the executable suite, and any information about the suite. (Author, Version, etc.)
  /// </summary>
  public abstract class ExtensionSuite {

	public String SuiteName { get; protected set; }

	/// <summary>
	/// The names of the extension suite authors.
	/// </summary>
	/// <value>The authors of the extension suite.</value>
	public String[] Authors { get; protected set; }

	/// <summary>
	/// The version number of the extension suite.
	/// This should typically be done in Major.Minor.Patch format. (Such as 1.0.3)
	/// Default is 0.0.1.
	/// </summary>
	/// <value>The version of the extension suite.</value>
	public String Version { get; protected set; }

	public String ConfigFile { get; protected set; }

	/// <summary>
	/// An array of id to extensions. Used internally to route requests.
	/// </summary>
	/// <value>The extensions.</value>
	public Extension[] Extensions { get; protected set; }

	/// <summary>
	/// The filename of the extension suite's main executable.
	/// </summary>
	/// <value>The filename.</value>
	public String Filename { get; protected set; }

	public ExtensionSuite() {
	  this.SuiteName = "Extension Suite";
	  this.Authors = new String[] { "Unknown Author" };
	  this.Version = "0.0.1";
	  this.Extensions = new Extension[0];
	}

	protected TcpClient GetConnection(int port) {
	  return new TcpClient("127.0.0.1", port);
	}

	/// <summary>
	/// Prepares the suite by connecting back to the bot and sending, line by line, extension ids and names.
	/// The bot uses this to get an initial list of what's available in a suite, rebuilding a suite object in the registry.
	/// </summary>
	/// <param name="port">Port of the bot's open comm port.</param>
	public abstract void PrepareSuite(int port);

  }
}

