using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;


using Ostenvighx.Suibhne;
using Ostenvighx.Suibhne.Networks.Base;

using Nini.Config;
using Ostenvighx.Suibhne.Extensions;

// TODO: Create Parrot extension to test regex/extension handling
namespace Launcher {
    class Program {
        static void Main(string[] args) {

            ExtensionSystem registry = new ExtensionSystem(Environment.CurrentDirectory + "/extensions.ini");

            try {
                IniConfigSource systemConfig = new IniConfigSource(Environment.CurrentDirectory + "/suibhne.ini");

                String configRoot = systemConfig.Configs["Suibhne"].GetString("ConfigurationRoot", Environment.CurrentDirectory + "/Configuration/");

                String networkRootDirectory = configRoot + systemConfig.Configs["Suibhne"].GetString("NetworkRootDirectory", Environment.CurrentDirectory + "/Configuration/Networks/");
                String[] networkDirectories = Directory.GetDirectories(networkRootDirectory);

                CreateNetworks(registry, networkDirectories);
                
            }

            catch (FileNotFoundException fnfe) {
                Console.WriteLine("Network configuration file not found: " + fnfe.Message);
            }

            catch (Exception e) {
                Console.WriteLine("Exception thrown: " + e);
            }

            Console.ReadLine();
        }

        static void CreateNetworks(ExtensionSystem registry, String[] servers) {
            foreach (String networkDirectory in servers) {
                if (File.Exists(networkDirectory + "/disabled"))
                    continue;

                NetworkBot network = new NetworkBot(networkDirectory, registry);
                network.Connect();
            }
        }
    }
}
