using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

using System.Diagnostics;
using System.Threading;
using System.Net.Sockets;
using System.Text;
using Ostenvighx.Suibhne.Networks.Base;
using Newtonsoft.Json.Linq;
using Ostenvighx.Suibhne.Commands;

using System.Data;
using System.Data.SQLite;

using Ostenvighx.Suibhne.Events;

namespace Ostenvighx.Suibhne.Extensions {

    [Script("extensions")]
    /// <summary>
    /// The extension registry connects an IrcBot and a set of extension suites together.
    /// It goes through the ExtensionDirectories directory defined in the bot configuration and
    /// searches through directories for the extension INI files.
    /// </summary>
    public class ExtensionSystem {

        private static ExtensionSystem instance;

        public static SQLiteConnection Database;

        // Used for timing stuffs
        private Timer t;

        public static ExtensionSystem Instance {
            get {
                if (instance == null) {
                    instance = new ExtensionSystem();
                }
                return instance;
            }
        }

        public Guid Identifier {
            get { return Instance.GetType().GUID; }
        }

        public int ConnectedExtensions { get; private set; }

        internal Dictionary<Guid, ExtensionMap> Extensions;
        internal List<Guid> UserEventHandlers;
        internal List<Guid> MessageHandlers;

        protected ExtensionServer Server;

        public delegate void ExtensionSystemEvent();
        public event ExtensionSystemEvent AllExtensionsReady;

        private ExtensionSystem() {
            if (File.Exists(Core.SystemConfig.SavePath)) {
                // Get some basic info about config file
                Core.ConfigLastUpdate = File.GetLastWriteTime(Core.SystemConfig.SavePath);
            }

            this.ConnectedExtensions = 0;
            this.Extensions = new Dictionary<Guid, ExtensionMap>();
            this.UserEventHandlers = new List<Guid>();
            this.MessageHandlers = new List<Guid>();

            Server = new ExtensionServer();
            Server.OnDataRecieved += HandleIncomingData;
            Server.OnSocketCrash += ShutdownExtension;
            Server.Start();

            LoadExtensionData();
            StartExtensions();
        }

        public static void Initialize() {
            if (instance == null)
                instance = new ExtensionSystem();
        }

        internal void ShutdownExtension(Guid id) {
            ExtensionMap extension = this.Extensions[id];

            Core.Log("Extension '" + extension.Name + "' is being shutdown. Resetting the references for it.", LogType.EXTENSIONS);
            JObject shutdown = new JObject();
            shutdown.Add("event", "extension.shutdown");

            extension.Send(Encoding.UTF32.GetBytes(shutdown.ToString()));

            extension.Socket = null;
            extension.Ready = false;

            Extensions[id] = extension;
        }

        internal void ShutdownExtension(Socket s) {
            foreach(Guid extensionID in this.Extensions.Keys) {

                ExtensionMap extension = this.Extensions[extensionID];

                // If this extension isn't ready or it's socket is not connected, just skip it
                if (!extension.Ready || extension.Socket == null)
                    continue;

                if (extension.Socket.RemoteEndPoint == s.RemoteEndPoint) {
                    ShutdownExtension(extension.Identifier);
                    return;
                }
            }
        }

        /// <summary>
        /// Loads the available extension guids into the allocated dictionary,
        /// getting it ready for further processing.
        /// </summary>
        private void LoadExtensionData() {
            if (Core.ConfigDirectory == "")
                throw new Exception("Config directory derp?");

            String[] directories = Directory.GetDirectories(Core.ConfigDirectory + "Extensions/");
            try {
                foreach (String extensionDir in directories) {
                    Core.Log("Loading directory.. " + extensionDir, LogType.DEBUG);

                    Guid extID = Guid.Parse(new DirectoryInfo(extensionDir).Name);

                    Core.Log("Loading information for extension: " + extID, LogType.EXTENSIONS);

                    this.Extensions.Add(extID, new ExtensionMap() { Identifier = extID });
                }
            }

            catch (FormatException fe) {
                // extension directory not named for guid
                Core.Log(fe.StackTrace, LogType.DEBUG);
            }

            Core.Log("All extensions primed.", LogType.EXTENSIONS);
            Core.Log("", LogType.EXTENSIONS);
        }

        protected void StartExtensions() {
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

                Core.Log("Trying to start " + filename, LogType.DEBUG);

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.WorkingDirectory = extDir;
                psi.FileName = filename;
                psi.Arguments = "--launch";
                psi.UseShellExecute = true;

                Process.Start(psi);
            }

            
            Core.Log("All extensions have been asked to start. Counter initialized.", LogType.EXTENSIONS);

            t = null;

            // Start a timer to check the progress of extension activation in 45 seconds
            t = new Timer((obj) => {
                CheckExtensionsStatus();
                t.Dispose();
            }, null, 45000, System.Threading.Timeout.Infinite);
        }

        public void HandleCommand(NetworkBot conn, Message msg) {
            CommandManager.Instance.HandleCommand(conn, msg);
        }

        private void HandleExtensionActivation(JObject EVENT, Socket sock) {

            Guid id = EVENT["extid"].ToObject<Guid>();

            if (!Extensions.ContainsKey(id))
                return;

            ExtensionMap extension = Extensions[id];
            extension.Name = EVENT["name"].ToString();

            ConnectedExtensions++;

            Core.Log("Activating extension: " + extension.Name, LogType.EXTENSIONS);

            if (extension.Socket == null) {
                extension.Socket = sock;
            }

            extension.Ready = true;

            Extensions[extension.Identifier] = extension;

            if (EVENT["required_events"] != null) {
                // Extension hooks to events system
                if (! Suibhne.Events.EventManager.VerifyCanSupport((EVENT["required_events"].ToObject<string[]>()))) {
                    // Abort activation, we don't have everything the extension is asking for.
                    // extension.Send();
                    return;
                }

                string[] required = (EVENT["required_events"] as JArray).ToObject<string[]>();
                string[] optional = (EVENT["optional_events"] as JArray).ToObject<string[]>();

                string[] all_extension_events = required.Union(optional).ToArray<String>();

                EventManager.UpdateExtensionSupport(id, all_extension_events);
            }

            if(Extensions.Count == ConnectedExtensions) {
                FinishConnectionProcess();
            }
        }

        /// <summary>
        /// Checks if all the extensions have started.
        /// If not, then this accepts that not all of them has started, throws off an error,
        /// then continues with what it has.
        /// </summary>
        private void CheckExtensionsStatus() {
            if(Extensions.Count != ConnectedExtensions) {
                Core.Log("Error: It's been a while and not all the extensions have responded. Hunting those down now, but continuing without them.", LogType.EXTENSIONS);

                // TODO: Figure out which extensions haven't started here, retry hooking them later
            }

            FinishConnectionProcess();
        }

        private void FinishConnectionProcess() {

            // Make sure the timer is stopped
            if(t != null)
                t.Dispose();

            Core.Log("All of the extensions are now connected.");
            if (instance.AllExtensionsReady != null)
                instance.AllExtensionsReady();
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
            #endregion

            try {
                if(ev["event"].ToString().ToLower() == "extension_activation") {
                    HandleExtensionActivation(ev, sock);
                    return;
                }

                string[] eventNameParts = ev["event"].ToString().ToLower().Split('_');
                string eventHandler = "";
                foreach(string eventPart in eventNameParts)
                    eventHandler += eventPart.Substring(0, 1).ToUpper() + eventPart.Substring(1);

                Type t = Type.GetType("Ostenvighx.Suibhne.Events.Handlers." + eventHandler);
                object handler = Activator.CreateInstance(t);

                (handler as Suibhne.Events.Handlers.EventHandler).HandleEvent(ev);

                /*
                #region Handle Code Response
                switch (ev["event"].ToString().ToLower()) {
                    case "extension_shutdown":
                        sock.Shutdown(SocketShutdown.Both);
                        sock.Close();
                        ShutdownExtension(sock);
                        return;

                    case "message_send":
                        try {
                            Guid locationID = ev["location"]["id"].ToObject<Guid>();
                            
                            Location location = LocationManager.GetLocationInfo(locationID);
                            if (location == null) 
                                break;

                            Message msg = new Message(locationID, new User(extension.Name), ev["contents"].ToString());
                            NetworkBot bot;
                            if (Message.IsPrivateMessage((Reference.MessageType) ev["location"]["type"].ToObject<byte>())) {
                                bot = Core.Networks[locationID];
                                msg.target = new User(ev["location"]["target"].ToString());
                            } else {
                                bot = Core.Networks[location.Parent];
                            }
                            
                            msg.type = (Reference.MessageType)((byte) ev["location"]["type"]);
                            bot.SendMessage(msg);                           
                        }

                        catch (KeyNotFoundException) {
                            // Network invalid or changed between requests
                        }

                        break;

                    default:
                        // Unknown response
                        Core.Log("Recieved unknown event code: " + ev["event"].ToString());
                        break;

                }
                #endregion
                */
            }
            catch (Exception e) {
                Core.Log("Extension error: " + e.Message, LogType.ERROR);
                Console.WriteLine(e);
            }
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

        /// <summary>
        /// Gets a list of all active extensions.
        /// </summary>
        /// <returns></returns>
        [Script("getActive")]
        public String[] GetActiveExtensions() {
            List<String> maps = new List<String>();
            foreach (ExtensionMap map in Extensions.Values) {
                if(map.Ready)
                    maps.Add(map.Name);
            }
            return maps.ToArray();
        }

        public void Shutdown() {
            foreach (ExtensionMap em in Extensions.Values) {
                ShutdownExtension(em.Identifier);
            }

            this.Server.Stop();
        }
    }
}

