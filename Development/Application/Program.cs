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

// TODO: Create Parrot extension to test regex/extension handling
namespace Launcher {
    class Program {
        static void Main(string[] args) {

            try {
                Core.ConfigLastUpdate = DateTime.Now;
                Core.SystemConfig = new IniConfigSource(Environment.CurrentDirectory + "/suibhne.ini");
                Core.SystemConfig.CaseSensitive = false;

                Core.SystemConfig.ExpandKeyValues();
                Core.ConfigDirectory = Core.SystemConfig.Configs["Directories"].GetString("ConfigurationRoot", Environment.CurrentDirectory + "/Configuration/");
                if (!File.Exists(Core.ConfigDirectory + "/system.json")) {
                    File.Create(Core.ConfigDirectory + "/system.json");
                    File.WriteAllText(Core.ConfigDirectory + "/system.json", "{}");
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
