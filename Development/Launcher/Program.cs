using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Raindrop.Api.Irc;
using Raindrop.Suibhne;
using Raindrop.Suibhne.Extensions;

namespace Launcher {
    class Program {
        static void Main(string[] args) {

            ServerConfig local = new ServerConfig();
            local.Username = "suibhne";
            local.Hostname = "localhost";
            local.DisplayName = "Suibhne";
            local.Port = 6667;
            local.Nickname = "Suibhne";
            local.AutoJoinChannels = new IrcLocation[] { new IrcLocation("#suibhne") };

            
            ExtensionRegistry registry = new ExtensionRegistry();

            IrcBot localhost = new IrcBot(local, registry);
            

            registry.AddBot(localhost);
            Console.WriteLine("Localhost ID: " + localhost.Identifier); 
            Console.WriteLine("Ext ID: " + registry.Identifier);

            localhost.Connect();
            Console.ReadLine();
        }
    }
}
