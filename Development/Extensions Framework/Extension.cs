using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Raindrop.Suibhne.Extensions {

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
    public abstract class Extension {

        public String Name { get; protected set; }

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

        protected Thread workThread;

        protected Socket conn;

        public Guid id;

        public enum ResponseCodes : byte {
            /// <summary>
            /// Request for extension name, runtype, and permissions.
            /// </summary>
            Activation = 1,

            SuiteDetails = 2,

            ExtensionPermissions = 3,

            /// <summary>
            /// Called when bot or extension requests a reactivation
            /// of extension.
            /// </summary>
            ExtensionRestart = 4,

            /// <summary>
            /// Called when a bot requests an extension be disabled 
            /// at runtime.
            /// </summary>
            ExtensionRemove = 5,

            ExtensionCommands = 6,

            /// <summary>
            /// Request for connection details.
            /// </summary>
            ConnectionDetails = 10,

            /// <summary>
            /// Connection initialized and starting up.
            /// Includes a connection ID for tracking.
            /// </summary>
            ConnectionStart = 11,

            /// <summary>
            /// Connection finished connecting.
            /// Includes a connection ID for tracking.
            /// </summary>
            ConnectionComplete = 12,

            /// <summary>
            /// Connection starting disconnect.
            /// Includes a connection ID for tracking.
            /// </summary>
            ConnectionEnding = 13,

            /// <summary>
            /// Connection finished disconnecting.
            /// Includes a connection ID for tracking.
            /// </summary>
            ConnectionStopped = 14,

            Message = 20
        };

        public Extension() {
            this.Name = "Extension";
            this.Authors = new String[] { "Unknown Author" };
            this.Version = "0.0.1";
            this.workThread = new Thread(new ThreadStart(RecieveData));
            this.conn = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// Prepares the suite by connecting back to the bot and sending, line by line, extension ids and names.
        /// The bot uses this to get an initial list of what's available in a suite, rebuilding a suite object in the registry.
        /// </summary>
        public virtual void Prepare() {

            try {
                conn.Connect("127.0.0.1", 6700);
                workThread.Start();
            }

            catch (Exception e) {
                Console.WriteLine("Failed to start extension.");
                Console.WriteLine(e);
            }

        }

        // TODO: Reimplement recieving data to make asynch?

        protected void RecieveData() {
            while (conn.Connected) {
                try {
                    var buffer = new byte[2048];
                    int received = conn.Receive(buffer, SocketFlags.None);
                    if (received == 0) return;
                    var data = new byte[received];
                    Array.Copy(buffer, data, received);

                    if((Extension.ResponseCodes) data[0] == Extension.ResponseCodes.Activation){
                        Console.WriteLine("Activation finished.");
                        byte[] idBytes = new byte[16];
                        Array.Copy(data, 1, idBytes, 0, 16);

                        id = new Guid(idBytes);

                        byte[] nameAsBytes = Encoding.ASCII.GetBytes(Name);
                        byte[] dataToSend = new byte[17 + nameAsBytes.Length];
                        
                        dataToSend[0] = (byte) Extension.ResponseCodes.SuiteDetails;
                        Array.Copy(idBytes, 0, dataToSend, 1, 16);
                        Array.Copy(nameAsBytes, 0, dataToSend, 17, nameAsBytes.Length);

                        conn.Send(dataToSend);
                    }
                }

                catch(Exception e) {
                    Console.WriteLine(e);
                }
           
            }
        }
    }
}

