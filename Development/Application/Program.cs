using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;

using Ostenvighx.Suibhne;
using Ostenvighx.Suibhne.Extensions;
using Ostenvighx.Suibhne.Networks.Base;

using Nini.Config;
using Ostenvighx.Suibhne.Scripting;
using Newtonsoft.Json.Linq;

namespace Launcher {
    class Program {
        static void Main(string[] args) {

            try {
                Core.ConfigLastUpdate = DateTime.Now;
                Core.SystemConfig = new IniConfigSource(Environment.CurrentDirectory + "/suibhne.ini");
                Core.SystemConfig.CaseSensitive = false;

                Core.SystemConfig.ExpandKeyValues();
                Core.ConfigDirectory = Core.SystemConfig.Configs["Directories"].GetString("ConfigurationRoot", Environment.CurrentDirectory + "/Configuration/");
                if (Core.ConfigDirectory[Core.ConfigDirectory.Length - 1] != '/') {
                    Core.ConfigDirectory += "/";
                    Core.SystemConfig.Configs["Directories"].Set("ConfigurationRoot", Core.ConfigDirectory);
                    Core.SystemConfig.Save();
                }
                if (!File.Exists(Core.ConfigDirectory + "/system.sns")) {
                    File.Create(Core.ConfigDirectory + "/system.sns");
                    File.WriteAllText(Core.ConfigDirectory + "/system.sns", Convert.ToBase64String(Encoding.UTF8.GetBytes("{}")));
                }
            }

            catch (Exception) {
                return;
            }

            Scripting.GatherScriptNodes();

            try {
                String networkRootDirectory = Core.SystemConfig.Configs["Directories"].GetString("NetworkRootDirectory", Environment.CurrentDirectory + "/Configuration/Networks/");
                String[] networkDirectories = Directory.GetDirectories(networkRootDirectory);

                CreateNetworks(networkDirectories);
                
            }

            catch (FileNotFoundException fnfe) {
                Console.WriteLine("Network configuration file not found: " + fnfe.Message);
            }

            catch (Exception e) {
                Console.WriteLine("Exception thrown: " + e);
            }

            Console.ReadLine();
        }

        static void CreateNetworks(String[] servers) {
            foreach (String networkDirectory in servers) {
                if (File.Exists(networkDirectory + "/disabled"))
                    continue;

                NetworkBot network = new NetworkBot(networkDirectory);
                if (network.Ready) {
                    network.Connect();
                }
            }
        }
    }
}
