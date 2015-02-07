using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Ostenvighx.Suibhne.Extensions {

  public enum RequestCode : byte {
	Unknown = 0,

	Activation = 1,

	ExtensionName = 10,
	ExtensionVersion = 11,
	ExtensionUpdateTime = 12
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

    protected Thread workThread;

    protected TcpClient client;
    protected StreamWriter output;
    protected StreamReader input;

    public Guid id;

	public ExtensionSuite() {
	  this.SuiteName = "Extension Suite";
	  this.Authors = new String[] { "Unknown Author" };
	  this.Version = "0.0.1";
	  this.Extensions = new Extension[0];
      this.workThread = new Thread(new ThreadStart(RecieveData));
	}

	/// <summary>
	/// Prepares the suite by connecting back to the bot and sending, line by line, extension ids and names.
	/// The bot uses this to get an initial list of what's available in a suite, rebuilding a suite object in the registry.
	/// </summary>
	/// <param name="port">Port of the bot's open comm port.</param>
    public void PrepareSuite() {
        client = new TcpClient();
        try {
            client.Connect("127.0.0.1", 6700);
            client.ReceiveBufferSize = 1024;

            NetworkStream stream = client.GetStream();
            output = new StreamWriter(stream);
            output.AutoFlush = true;
            output.WriteLine("REGISTER: " + SuiteName);

            input = new StreamReader(stream);

            workThread.Start();
        }

        catch (Exception e) {
            Console.WriteLine("Failed to start extension.");
            Console.WriteLine(e);
            Thread.Sleep(5000);

        }
        
    }

    protected void UnloadSuite() {
        try {
            output.WriteLine("ext.shutdown");
            workThread.Abort();
            client.Close();
        }

        catch (Exception e) {
            Console.WriteLine(e);

            Thread.Sleep(10000);
        }
        
    }

    protected void RecieveData() {
        while (client.Connected) {
            try {
                var buffer = new byte[2048];
                int received = client.Client.Receive(buffer, SocketFlags.None);
                if (received == 0) return;
                var data = new byte[received];
                Array.Copy(buffer, data, received);
                string text = Encoding.ASCII.GetString(data);
                Console.WriteLine(text);

                if (text.ToLower().Trim().StartsWith("accepted")) {
                    id = Guid.Parse(text.Substring(9));

                    output.WriteLine("id.set " + id + " " + SuiteName);
                }
            }

            catch(Exception e) {
                Console.WriteLine(e);
            }
           
        }
    }
  }
}

