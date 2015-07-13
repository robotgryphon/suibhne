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

        protected ExtensionServer Server;

        public event Events.ExtensionMapEvent OnExtensionConnected;
        public event Events.ExtensionMapEvent OnExtensionStopped;

        private ExtensionSystem() {
            if (File.Exists(Core.SystemConfig.SavePath)) {
                // Get some basic info about config file
                Core.ConfigLastUpdate = File.GetLastWriteTime(Core.SystemConfig.SavePath);
            }

            this.Extensions = new Dictionary<Guid, ExtensionMap>();

            InitializeExtensions();

            Server = new ExtensionServer();
            Server.OnDataRecieved += HandleIncomingData;
            Server.OnSocketCrash += ShutdownExtension;
            Server.Start();
        }

        internal void ShutdownExtension(Guid id) {
            ExtensionMap extension = this.Extensions[id];

            Core.Log("Extension '" + extension.Name + "' is being shutdown. Resetting the references for it.", LogType.EXTENSIONS);
            ExtensionHelper.SendShutdownRequest(extension);
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

        protected void InitializeExtensions() {
            // Get ExtensionDirectories available via directory name
            String ExtensionsRootDirectory = Core.SystemConfig.Configs["Directories"].GetString("ExtensionsRootDirectory", Environment.CurrentDirectory + "/Extensions/");

            DataTable extensions = new DataTable();
            try {
                ExtensionSystem.Database.Open();

                SQLiteCommand c = new SQLiteCommand(ExtensionSystem.Database);
                c.CommandText = "SELECT * FROM Extensions WHERE Enabled=1;";

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

                    String extensionExtension = Path.GetExtension(extension["InstallPath"].ToString());
                    if (extensionExtension.ToLower() == ".exe") {
                        Core.Log("Starting extension " + map.Name + " (" + extension["InstallPath"] + ") ...", LogType.EXTENSIONS);

                        try {
                            ProcessStartInfo psi = new ProcessStartInfo();
                            psi.WorkingDirectory = new DirectoryInfo(extension["InstallPath"].ToString()).Parent.FullName;
                            psi.UseShellExecute = true;
                            psi.FileName = extension["InstallPath"].ToString();
                            psi.Arguments = "--launch";
                            
                            Process.Start(psi);
                        }

                        catch (Exception e) {
                            Core.Log("Caught exception trying to start extension: " + e);
                        }
                        
                    }
                }

            }

            catch (Exception e) {

            }

            finally {
                ExtensionSystem.Database.Close();
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
                        ShutdownExtension(sock);
                        return;

                    case Responses.Message:
                        Message msg = Extension.ParseMessage(data);

                        try {
                            foreach (NetworkBot bot in Core.Networks.Values) {
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
                    maps.Add(map.Name);
            }
            return maps.ToArray();
        }

        public void Shutdown() {
            foreach (ExtensionMap em in Extensions.Values) {
                ExtensionHelper.SendShutdownRequest(em);
            }

            this.Server.Stop();
        }
    }
}

