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

namespace Ostenvighx.Suibhne.Extensions {

    [Script("extensions")]
    /// <summary>
    /// The extension registry connects an IrcBot and a set of extension suites together.
    /// It goes through the ExtensionDirectories directory defined in the bot configuration and
    /// searches through directories for the extension INI files.
    /// </summary>
    public class ExtensionSystem {

        public Guid Identifier;
        protected Dictionary<Guid, NetworkBot> bots;

        internal Dictionary<Guid, ExtensionMap> Extensions;

        [Script("startTime")]
        public DateTime StartTime;

        protected ExtensionServer Server;
        protected CommandManager Commands;

        internal DateTime ConfigLastUpdate;

        public ExtensionSystem() {
            this.bots = new Dictionary<Guid, NetworkBot>();
            this.Identifier = Guid.NewGuid();
            this.Commands = new CommandManager();

            if (File.Exists(Core.ExtensionConfigFilename)) {
                // Get some basic info about config file
                this.ConfigLastUpdate = File.GetLastWriteTime(Core.ExtensionConfigFilename);
            }

            this.Extensions = new Dictionary<Guid, ExtensionMap>();

            this.StartTime = DateTime.Now;

            InitializeExtensions();

            Server = new ExtensionServer();
            Server.OnDataRecieved += HandleIncomingData;
            Server.OnSocketCrash += HandleExtensionCrash;
            Server.Start();
        }

        #region Registry
        public void AddBot(NetworkBot bot) {
            if (!this.bots.ContainsKey(bot.Identifier))
                bots.Add(bot.Identifier, bot);
        }


        private void HandleExtensionCrash(Socket s) {
            foreach(Guid extID in this.Extensions.Keys) {
                ExtensionMap em = Extensions[extID];
                if (em.Socket == s) {
                    Core.Log("Extension '" + em.Name + "' has crashed. Resetting the references for it.", LogType.EXTENSIONS);
                    em.Socket = null;
                    em.Ready = false;

                    break;
                }
            }
        }

        protected void InitializeExtensions() {
            if (!File.Exists(Core.ExtensionConfigFilename))
                throw new FileNotFoundException("Config file not valid.");


            IniConfigSource MainExtensionConfiguration = new IniConfigSource(Core.ExtensionConfigFilename);

            Core.Log("Extension file last updated: " + File.GetLastWriteTime(Core.ExtensionConfigFilename), LogType.EXTENSIONS);

            // Get ExtensionDirectories available via directory name
            String ExtensionsRootDirectory = MainExtensionConfiguration.Configs["Extensions"].GetString("extensionDir", Environment.CurrentDirectory + "/Extensions/");

            ExtensionMap[] exts = ExtensionLoader.LoadExtensions(ExtensionsRootDirectory);
            foreach (ExtensionMap extension in exts) {
                Extensions.Add(extension.Identifier, extension);
            }

            Core.Log("All extensions loaded into system.", LogType.EXTENSIONS);

            Commands.MapCommands();
        }

        public void HandleCommand(NetworkBot conn, Message msg) {
            Commands.HandleCommand(this, conn, msg);
        }

        #endregion

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

                ExtensionMap suite = Extensions[origin];


                #region Handle Code Response
                switch (code) {

                    case Responses.Activation:
                        Core.Log("Activating extension: " + Extensions[origin].Name, LogType.EXTENSIONS);

                        if (suite.Socket == null) {
                            suite.Socket = sock;
                        }

                        suite.Ready = true;

                        Extensions[origin] = suite;
                        break;

                    case Responses.Details:
                        Console.WriteLine("Recieving extension details");
                        String suiteName = Encoding.UTF8.GetString(extraData);
                        suite.Name = suiteName;
                        break;

                    case Responses.Remove:
                        sock.Shutdown(SocketShutdown.Both);
                        sock.Close();
                        return;

                    case Responses.Message:
                        Message msg = new Message(Guid.Empty, new User(), "");
                        Guid destination;
                        byte type = 1;

                        Extension.ParseMessage(
                            data,
                            out origin,
                            out destination,
                            out type,
                            out msg.sender.DisplayName,
                            out msg.message);

                        msg.type = (Ostenvighx.Suibhne.Networks.Base.Reference.MessageType)type;
                        msg.locationID = destination;

                        try {
                            NetworkBot bot = bots[Core.NetworkLocationMap[destination]];
                            bot.SendMessage(msg);
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
                Core.Log("Extension callback: " + e.Message, LogType.ERROR);
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

