using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ostenvighx.Suibhne;
using Ostenvighx.Suibhne.Extensions;

namespace CLI {
    class Program {
        static void Main(string[] args) {

            Core.LoadConfiguration();
            Core.LoadNetworks();

            ExtensionSystem.Instance.Start();
            Ostenvighx.Suibhne.Commands.CommandManager.Instance.MapCommands();
            Core.StartNetworks();

            Console.ReadLine();
        }
    }
}
