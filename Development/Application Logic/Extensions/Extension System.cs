using Newtonsoft.Json.Linq;
using Ostenvighx.Suibhne.Commands;
using Ostenvighx.Suibhne.Events;
using Ostenvighx.Suibhne.Networks.Base;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Ostenvighx.Suibhne.Extensions {

    /// <summary>
    /// The extension registry connects an IrcBot and a set of extension suites together.
    /// It goes through the ExtensionDirectories directory defined in the bot configuration and
    /// searches through directories for the extension INI files.
    /// </summary>
    public class ExtensionSystem {

        internal Dictionary<Guid, ExtensionMap> Extensions;
        protected ExtensionServer Server;
        private static ExtensionSystem instance;

        // Used for timing stuffs
        private Timer t;

        private ExtensionSystem() {
            this.ConnectedExtensions = 0;
            this.Extensions = new Dictionary<Guid, ExtensionMap>();

            Server = new ExtensionServer();
            Server.OnDataRecieved += HandleIncomingData;
            Server.OnSocketCrash += ShutdownExtension;
            Server.Start();
        }

        public delegate void ExtensionSystemEvent();

        public event ExtensionSystemEvent AllExtensionsReady;

        public static ExtensionSystem Instance {
            get {
                if (instance == null) {
                    instance = new ExtensionSystem();
                }
                return instance;
            }
        }

        public int ConnectedExtensions { get; private set; }

        public Guid Identifier {
            get { return Instance.GetType().GUID; }
        }

        public static ExtensionMap GetExtension(Guid id) {
            if (instance.Extensions.ContainsKey(id))
                return instance.Extensions[id];

            return ExtensionMap.None;
        }

        /// <summary>
        /// Returns a list of all the extensions registered inside the database.
        /// </summary>
        /// <returns></returns>
        public static ExtensionMap[] GetExtensionList() {
            List<ExtensionMap> maps = new List<ExtensionMap>();

            //TODO: REIMPLEMENT WITH NEW/OLD SYSTEM

            return maps.ToArray();
        }

        public static void Initialize() {
            if (instance == null)
                instance = new ExtensionSystem();

            instance.LoadExtensionData();
            instance.StartExtensions();
        }

        /// <summary>
        /// Gets a list of all active extensions.
        /// </summary>
        /// <returns></returns>
        public String[] GetActiveExtensions() {
            List<String> maps = new List<String>();
            foreach (ExtensionMap map in Extensions.Values) {
                if (map.Ready)
                    maps.Add(map.Name);
            }
            return maps.ToArray();
        }

        public void HandleCommand(NetworkBot conn, Message msg) {
            CommandManager.Instance.HandleCommand(conn, msg);
        }

        public void Shutdown() {
            foreach (ExtensionMap em in Extensions.Values) {
                ShutdownExtension(em.Identifier);
            }

            this.Server.Stop();
        }

        // TODO: Optimize this function
        internal static Guid FindByName(String name) {
            foreach(ExtensionMap em in instance.Extensions.Values) {
                if(em.Name.ToLower() == name.ToLower()) {
                    return em.Identifier;
                }
            }

            throw new Exception("An extension with that name could not be found.");
        }

        internal static void ShutdownExtension(Guid id) {
            if (instance == null)
                return;

            if (!instance.Extensions.ContainsKey(id))
                throw new Exception("An extension with that identifier is not loaded into the system.");

            ExtensionMap extension = instance.Extensions[id];

            Core.Log("Extension '" + extension.Name + "' is being shutdown. Resetting the references for it.", LogType.EXTENSIONS);
            JObject shutdown = new JObject();
            shutdown.Add("event", "extension.shutdown");

            extension.Send(Encoding.UTF32.GetBytes(shutdown.ToString()));

            extension.Socket = null;
            extension.Ready = false;

            instance.Extensions[id] = extension;
        }

        internal static void ShutdownExtension(Socket s) {
            foreach (Guid extensionID in instance.Extensions.Keys) {
                ExtensionMap extension = instance.Extensions[extensionID];

                // If this extension isn't ready or it's socket is not connected, just skip it
                if (!extension.Ready || extension.Socket == null)
                    continue;

                if (extension.Socket.RemoteEndPoint == s.RemoteEndPoint) {
                    ShutdownExtension(extension.Identifier);
                    return;
                }
            }
        }

        protected void HandleIncomingData(Socket sock, byte[] data) {
            JObject ev;

            #region Set up decode

            try {
                string json = Encoding.UTF32.GetString(data);
                ev = JObject.Parse(json);
            }
            catch (Exception) {
                Core.Log("Error parsing data from extension.", LogType.ERROR);
                return;
            }

            #endregion Set up decode

            try {
                if (ev["event"].ToString().ToLower() == "extension_activation") {
                    HandleExtensionActivation(ev, sock);
                    return;
                }

                EventManager.HandleExtensionEvent(ev);
            }

            catch (Exception e) {
                Core.Log("Extension error: " + e.Message, LogType.ERROR);
                Console.WriteLine(e);
            }
        }

        /// <summary>
        /// Checks if all the extensions have started.
        /// If not, then this accepts that not all of them has started, throws off an error,
        /// then continues with what it has.
        /// </summary>
        private void CheckExtensionsStatus() {
            if (Extensions.Count != ConnectedExtensions) {
                if (!Core.DEBUG)
                    Core.Log("Error: It's been a while and not all the extensions have responded. Hunting those down now, but continuing without them.", LogType.EXTENSIONS);
                else
                    Core.Log("{Extensions startup skipped for now, forcing continue}", LogType.EXTENSIONS);

                // TODO: Figure out which extensions haven't started here, retry hooking them later
            }

            FinishConnectionProcess();
        }

        private void FinishConnectionProcess() {
            // Make sure the timer is stopped
            if (t != null)
                t.Dispose();

            Core.Log("All of the extensions are now connected.");
            if (AllExtensionsReady != null)
                AllExtensionsReady();
        }

        private void HandleExtensionActivation(JObject EVENT, Socket sock) {
            

            Core.Log("Activating extension: " + EVENT["name"], LogType.EXTENSIONS);

            if (EVENT["required_events"] != null) {
                // Extension hooks to events system
                if (!EventManager.VerifyCanSupport((EVENT["required_events"].ToObject<string[]>()))) {
                    
                    // Abort activation, we don't have everything the extension is asking for.
                    JObject error = new JObject();
                    error.Add("event", "extension_shutdown");
                    error.Add("reason", "The requested required events cannot be supported.");

                    sock.Send(Encoding.UTF32.GetBytes(error.ToString()));
                    return;
                }

                string[] required = (EVENT["required_events"] as JArray).ToObject<string[]>();
                string[] optional = (EVENT["optional_events"] as JArray).ToObject<string[]>();

                string[] all_extension_events = required.Union(optional).ToArray<String>();

                EventManager.UpdateExtensionSupport(EVENT["extid"].ToObject<Guid>(), all_extension_events);
            }

            if (Extensions.Count == ConnectedExtensions) {
                FinishConnectionProcess();
            }

            #region Set up extension references in system
            // Set up extension in system
            Guid id = EVENT["extid"].ToObject<Guid>();

            if (!Extensions.ContainsKey(id))
                return;

            ExtensionMap extension = Extensions[id];
            extension.Name = EVENT["name"].ToString();

            if (extension.Socket == null)
                extension.Socket = sock;

            ConnectedExtensions++;
            extension.Ready = true;
            Extensions[extension.Identifier] = extension;
            #endregion
        }

        /// <summary>
        /// Loads the available extension guids into the allocated dictionary,
        /// getting it ready for further processing.
        /// </summary>
        private void LoadExtensionData() {
            if (Core.ConfigDirectory == "" || Core.ConfigDirectory == null)
                throw new Exception("Config directory derp?");

            String[] directories = Directory.GetDirectories(Core.ConfigDirectory + "Extensions/");
            try {
                foreach (String extensionDir in directories) {
                    Debug.WriteLine("Loading extension from directory: " + extensionDir);
                    Guid extID = Guid.Parse(new DirectoryInfo(extensionDir).Name);
                    this.Extensions.Add(extID, new ExtensionMap() { Identifier = extID });
                }
            }
            catch (FormatException fe) {
                // extension directory not named for guid
                Debug.WriteLine(fe.StackTrace);
            }

            Core.Log("All extensions primed." + Environment.NewLine, LogType.EXTENSIONS);
        }

        private void StartExtensions() {
            foreach (String extDir in Directory.GetDirectories(Core.ConfigDirectory + "/Extensions/")) {
                // Start extension
                DirectoryInfo di = new DirectoryInfo(extDir);

                String filename = "";
                if (File.Exists(extDir + @"\" + di.Name + ".exe"))
                    filename = extDir + @"\" + di.Name + ".exe";

                if (File.Exists(extDir + @"\" + di.Name + ".jar"))
                    filename = extDir + @"\" + di.Name + ".jar";

                // If the extension main file isn't found, keep on going and ignore it
                if (filename == "")
                    continue;

                Debug.WriteLine("Trying to start extension with file " + filename, "Extensions");

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.WorkingDirectory = extDir;
                psi.FileName = filename;
                psi.Arguments = "--launch";
                psi.UseShellExecute = true;

                Process.Start(psi);
            }


            if (!Core.DEBUG) {
                Core.Log("All extensions have been asked to start. Counter initialized.", LogType.EXTENSIONS);
                t = null;

                // Start a timer to check the progress of extension activation in 45 seconds
                t = new Timer((obj) => {
                    CheckExtensionsStatus();
                    t.Dispose();
                }, null, 45000, System.Threading.Timeout.Infinite);
            } else {
                CheckExtensionsStatus();
            }
        }
    }
}