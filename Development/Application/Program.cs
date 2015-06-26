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

// TODO: Create Parrot extension to test regex/extension handling
namespace Launcher {
    class Program {
        static void Main(string[] args) {

            Core.SystemConfigFilename = Environment.CurrentDirectory + "/suibhne.ini";

            ScriptAttribute sa;
            foreach (Type type in Assembly.GetAssembly(typeof(ExtensionSystem)).GetTypes()) {
                if (type.GetCustomAttribute(typeof(ScriptAttribute)) != null) {
                    sa = (ScriptAttribute) type.GetCustomAttribute(typeof(ScriptAttribute));
                    Core.Log(">>> Found a node for '" + sa.key + "' on type '" + type.FullName + "'.");

                    Core.VariableNodes.Add(sa.key, type);
                    String rootTypeKey = sa.key;

                    foreach (MethodInfo mi in type.GetMethods()) {
                        if (mi.GetCustomAttribute(typeof(ScriptAttribute)) != null) {
                            sa = (ScriptAttribute)mi.GetCustomAttribute(typeof(ScriptAttribute));
                            Core.Log(">>> Found a node for '" + sa.key + "' on type '" + type.FullName + ":" + mi.Name + "'.");

                            Core.VariableNodes.Add(rootTypeKey + "." + sa.key, mi);
                        }
                    }

                    foreach (FieldInfo pi in type.GetFields()) {
                        if (pi.GetCustomAttribute(typeof(ScriptAttribute)) != null) {
                            sa = (ScriptAttribute) pi.GetCustomAttribute(typeof(ScriptAttribute));
                            Core.Log(">>> Found a node for '" + sa.key + "' on type '" + type.FullName + ":" + pi.Name + "'.");

                            Core.VariableNodes.Add(rootTypeKey + "." + sa.key, pi);
                        }
                    }
                }
            }

            try {
                IniConfigSource systemConfig = new IniConfigSource(Core.SystemConfigFilename);

                Core.ConfigurationRootDirectory = systemConfig.Configs["Directories"].GetString("ConfigurationRoot", Environment.CurrentDirectory + "/Configuration/");

                String networkRootDirectory = Core.ConfigurationRootDirectory + systemConfig.Configs["Directories"].GetString("NetworkRootDirectory", Environment.CurrentDirectory + "/Configuration/Networks/");
                String[] networkDirectories = Directory.GetDirectories(networkRootDirectory);

                CreateNetworks(ExtensionSystem.Instance, networkDirectories);
                
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
