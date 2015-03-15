using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Raindrop.Api.Irc;
using Raindrop.Suibhne;

using Nini.Config;

namespace Launcher {
    class Program {
        static void Main(string[] args) {

            ExtensionSystem registry = new ExtensionSystem();

            try {
                IniConfigSource systemConfig = new IniConfigSource(Environment.CurrentDirectory + "/suibhne.ini");

                String serverRootDirectory = systemConfig.Configs["Suibhne"].GetString("ServerRootDirectory", Environment.CurrentDirectory + "/Configuration/Servers/");
                String[] serverDirectories = Directory.GetDirectories(serverRootDirectory);

                foreach (String serverDirectory in serverDirectories) {
                    String serverConfigFile = serverDirectory + "/connection.ini";
                    Console.WriteLine("Loading configuration for: " + serverConfigFile);

                    ServerConfig servConfig = ServerConfig.LoadFromFile(serverConfigFile);
                    IrcBot server = new IrcBot(servConfig, registry);

                    registry.AddBot(server);
                    server.Connect();
                }
            }

            catch (FileNotFoundException fnfe) {
                Console.WriteLine("Server configuration file not found: " + fnfe.Message);
            }

            catch (Exception e) {
                Console.WriteLine("Exception thrown: " + e);
            }

            Console.ReadLine();
        }
    }
}
