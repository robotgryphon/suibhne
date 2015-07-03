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

namespace Ostenvighx.Suibhne.Extensions {

    [Script("extensions")]
    /// <summary>
    /// The extension registry connects an IrcBot and a set of extension suites together.
    /// It goes through the ExtensionDirectories directory defined in the bot configuration and
    /// searches through directories for the extension INI files.
    /// </summary>
    public class ExtensionSystem {

        private static ExtensionSystem instance;

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

        protected Dictionary<Guid, NetworkBot> bots;

        internal Dictionary<Guid, ExtensionMap> Extensions;

        protected ExtensionServer Server;

        public event Events.ExtensionMapEvent OnExtensionConnected;
        public event Events.ExtensionMapEvent OnExtensionStopped;

        private ExtensionSystem() {
            this.bots = new Dictionary<Guid, NetworkBot>();

            if (File.Exists(Core.SystemConfig.SavePath)) {
                // Get some basic info about config file
                Core.ConfigLastUpdate = File.GetLastWriteTime(Core.SystemConfig.SavePath);
            }

            this.Extensions = new Dictionary<Guid, ExtensionMap>();

            InitializeExtensions();

            Server = new ExtensionServer();
            Server.OnDataRecieved += HandleIncomingData;
            Server.OnSocketCrash += ShutdownExtensionBySocket;
            Server.Start();
        }

        public void AddBot(NetworkBot bot) {
            if (!this.bots.ContainsKey(bot.Identifier))
                bots.Add(bot.Identifier, bot);
        }


        internal void ShutdownExtensionBySocket(Socket s) {
            foreach(Guid extID in this.Extensions.Keys) {
                ExtensionMap em = Extensions[extID];

                // If socket already shut down, exit
                if(em.Socket == null)
                    break;
                
                if (em.Socket.RemoteEndPoint == s.RemoteEndPoint) {
                    Core.Log("Extension '" + em.Name + "' is being shutdown. Resetting the references for it.", LogType.EXTENSIONS);
                    ExtensionHelper.SendShutdownRequest(em);
                    em.Socket = null;
                    em.Ready = false;

                    Extensions[extID] = em;
                    break;
                }
            }
        }

        protected void InitializeExtensions() {
            if (!File.Exists(Core.SystemConfig.SavePath))
                throw new FileNotFoundException("Config file not valid.");

            // Get ExtensionDirectories available via directory name
            String ExtensionsRootDirectory = Core.SystemConfig.Configs["Directories"].GetString("ExtensionsRootDirectory", Environment.CurrentDirectory + "/Extensions/");

            string encodedFile = File.ReadAllText(Core.ConfigDirectory + "/system.sns");
            string decodedFile = Encoding.UTF8.GetString(Convert.FromBase64String(encodedFile));
            JObject config = JObject.Parse(decodedFile);

            foreach (JProperty ext in config["Extensions"]) {
                Core.Log("Loading extension information: " + ext.Name, LogType.EXTENSIONS);
                ExtensionMap map = new ExtensionMap();
                map.Ready = false;
                map.Socket = null;
                map.Name = ext.Name;
                map.Identifier = Guid.Parse((String)ext.Value["Identifier"]);

                Extensions.Add(map.Identifier, map);

                String extensionExtension = Path.GetExtension(ext.Value["InstallPath"].ToString());
                if (extensionExtension.ToLower() == ".exe") {
                    Core.Log("Starting extension " + ext.Name + "...", LogType.EXTENSIONS);

                    // Process.Start(ext.Value["InstallPath"].ToString(), "--launch " + Core.ConfigDirectory);
                }
            }

            Core.Log("All extensions loaded into system.", LogType.EXTENSIONS);

            CommandManager.Instance.MapCommands();
        }

        public void HandleCommand(NetworkBot conn, Message msg) {
            CommandManager.Instance.HandleCommand(conn, msg);
        }

        protected void HandleIncomingData(Socket sock, byte[] data) {
            Responses code = (Responses)data[0];
            byte[] guidBytes = new byte[16];
            Array.Copy(data, 1, guidBytes, 0, 16);

            Guid origin = new Guid(guidBytes);
            byte[] extraData = new byte[0];
            if (data.Length > 17) {
                extraData = new byte[data.Length - 17];
                Array.Copy(data, 17, extraData, 0, extraData.Length);
            }

            // Get the extension suite off the returned Identifier first
            try {

                ExtensionMap extension = Extensions[origin];


                #region Handle Code Response
                switch (code) {

                    case Responses.Activation:
                        Core.Log("Activating extension: " + Extensions[origin].Name, LogType.EXTENSIONS);

                        if (extension.Socket == null) {
                            extension.Socket = sock;
                        }

                        extension.Ready = true;

                        Extensions[origin] = extension;
                        break;

                    case Responses.Details:
                        Core.Log("Recieved extension details from " + extension.Name + ": " + Encoding.UTF8.GetString(extraData), LogType.EXTENSIONS);
                        break;

                    case Responses.Remove:
                        sock.Shutdown(SocketShutdown.Both);
                        sock.Close();
                        ShutdownExtensionBySocket(sock);
                        return;

                    case Responses.Message:
                        Message msg = Extension.ParseMessage(data);

                        try {
                            foreach (NetworkBot bot in bots.Values) {
                                if(bot.IsListeningTo(msg.locationID)){
                                    bot.SendMessage(msg);
                                }
                            }
                            
                        }

                        catch (KeyNotFoundException) {
                            // Network invalid or changed between requests
                        }

                        break;

                    default:
                        // Unknown response

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
                    maps.Add(map.Name + ": " + map.Identifier);
            }
            return maps.ToArray();
        }

    }
}

