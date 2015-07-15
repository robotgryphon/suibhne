using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

using Nini.Config;

using System.Diagnostics;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Ostenvighx.Suibhne;
using Ostenvighx.Suibhne.Networks.Base;
using Newtonsoft.Json.Linq;
using Ostenvighx.Suibhne.Commands;

using System.Data;
using System.Data.SQLite;

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

        public static String ConfigRoot;

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

        internal Dictionary<Guid, ExtensionMap> Extensions;
        internal List<Guid> UserEventHandlers;
        internal List<Guid> MessageHandlers;

        protected ExtensionServer Server;

        public event Events.ExtensionMapEvent OnExtensionConnected;
        public event Events.ExtensionMapEvent OnExtensionStopped;

        private ExtensionSystem() {
            if (File.Exists(Core.SystemConfig.SavePath)) {
                // Get some basic info about config file
                Core.ConfigLastUpdate = File.GetLastWriteTime(Core.SystemConfig.SavePath);
            }

            ConfigRoot = Core.SystemConfig.Configs["Directories"].GetString("ExtensionsRootDirectory", Environment.CurrentDirectory + "/Extensions/");

            this.Extensions = new Dictionary<Guid, ExtensionMap>();
            this.UserEventHandlers = new List<Guid>();
            this.MessageHandlers = new List<Guid>();

            LoadExtensionData();
            CommandManager.Instance.MapCommands();

            AutostartExtensions();

            Server = new ExtensionServer();
            Server.OnDataRecieved += HandleIncomingData;
            Server.OnSocketCrash += ShutdownExtension;
            Server.Start();
        }

        internal void ShutdownExtension(Guid id) {
            ExtensionMap extension = this.Extensions[id];

            Core.Log("Extension '" + extension.Name + "' is being shutdown. Resetting the references for it.", LogType.EXTENSIONS);
            JObject shutdown = new JObject();
            shutdown.Add("responseCode", "extension.shutdown");

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

        protected void AutostartExtensions() {
            foreach (String extDir in Directory.GetDirectories(ConfigRoot)) {
                // Start extension
                DirectoryInfo di = new DirectoryInfo(extDir);

                Core.Log("Trying to start " + di.Name + ".", LogType.EXTENSIONS);
                String filename = "";
                if (File.Exists(extDir + @"\" + di.Name + ".exe"))
                    filename = extDir + @"\" + di.Name + ".exe";

                if (File.Exists(extDir + @"\" + di.Name + ".jar"))
                    filename = extDir + @"\" + di.Name + ".jar";


                ProcessStartInfo psi = new ProcessStartInfo();
                psi.WorkingDirectory = extDir;
                psi.FileName = filename;
                psi.Arguments = "--launch";
                psi.UseShellExecute = true;

                Process.Start(psi);
            }
        }

        protected void LoadExtensionData() {
            DataTable extensions = new DataTable();
            try {
                ExtensionSystem.Database.Open();

                SQLiteCommand c = new SQLiteCommand(ExtensionSystem.Database);
                c.CommandText = "SELECT * FROM Extensions;";

                SQLiteDataReader r = c.ExecuteReader();
                extensions.Load(r);

                foreach (DataRow extension in extensions.Rows) {
                    // Extension information gotten
                    ExtensionMap map = new ExtensionMap();
                    map.Ready = false;
                    map.Socket = null;
                    map.Name = extension["Name"].ToString();
                    map.Identifier = Guid.Parse((String) extension["Identifier"]);

                    Core.Log("Got information from extension " + map.Name);

                    Extensions.Add(map.Identifier, map);

                    if (extension["HandlesUserEvents"].ToString() == "1") UserEventHandlers.Add(map.Identifier);
                    if (extension["HandlesMessages"].ToString() == "1") MessageHandlers.Add(map.Identifier);
                    
                }

            }

            catch (Exception e) {

            }

            finally {
                ExtensionSystem.Database.Close();
            }

            Core.Log("All extensions loaded into system.", LogType.EXTENSIONS);

            // If we have any event handlers
            if (UserEventHandlers.Count > 0) {
                foreach (NetworkBot b in Core.Networks.Values) {
                    b.Network.OnUserJoin += ExtensionEventHandlers.HandleUserJoin;
                    b.Network.OnUserLeave += ExtensionEventHandlers.HandleUserLeave;
                    b.Network.OnUserQuit += ExtensionEventHandlers.HandleUserQuit;

                    b.Network.OnUserDisplayNameChange += ExtensionEventHandlers.HandleUserNameChange;
                }
            }

            if (MessageHandlers.Count > 0) {
                foreach (NetworkBot b in Core.Networks.Values) {
                    b.Network.OnMessageRecieved += ExtensionEventHandlers.HandleMessageRecieved;
                }
            }
        }

        public void HandleCommand(NetworkBot conn, Message msg) {
            CommandManager.Instance.HandleCommand(conn, msg);
        }

        protected void HandleIncomingData(Socket sock, byte[] data) {               

            // Get the extension suite off the returned Identifier first
            try {

                string json = Encoding.UTF32.GetString(data);
                JObject ev = JObject.Parse(json);

                ExtensionMap extension = Extensions[ev["extid"].ToObject<Guid>()];


                #region Handle Code Response
                switch (ev["responseCode"].ToString().ToLower()) {

                    case "extension.activate":
                        Core.Log("Activating extension: " + extension.Name, LogType.EXTENSIONS);

                        if (extension.Socket == null) {
                            extension.Socket = sock;
                        }

                        extension.Ready = true;

                        Extensions[extension.Identifier] = extension;
                        break;

                    case "extension.shutdown":
                        sock.Shutdown(SocketShutdown.Both);
                        sock.Close();
                        ShutdownExtension(sock);
                        return;

                    case "message.send":
                        try {
                            DataRow location = Utilities.GetLocationEntry(Guid.Parse(ev["location"]["id"].ToString()));
                            if (location == null) break;

                            NetworkBot bot = Core.Networks[Guid.Parse(location["ParentId"].ToString())];
                            Message msg = new Message( Guid.Parse(location["Identifier"].ToString()), new User(extension.Name), ev["contents"].ToString() );
                            msg.type = (Reference.MessageType)((byte) ev["location"]["type"]);
                            bot.SendMessage(msg);                           
                        }

                        catch (KeyNotFoundException) {
                            // Network invalid or changed between requests
                        }

                        break;

                    default:
                        // Unknown response
                        Core.Log("Recieved unknown response code: " + ev["responseCode"].ToString());
                        break;

                }
                #endregion
            }
            catch (Exception e) {
                Core.Log("Extension error: " + e.Message, LogType.ERROR);
                Console.WriteLine(e);
            }
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

