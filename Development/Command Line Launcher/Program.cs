using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ostenvighx.Suibhne;
using Ostenvighx.Suibhne.Extensions;
using Ostenvighx.Suibhne.Events;

namespace CLI {
    class Program {
        static void Main(string[] args) {

            Core.LoadConfiguration();
            Core.LoadNetworks();

            EventManager.Initialize();
            ExtensionSystem.Initialize();

            // Core.LoadNetworks();


            Console.ReadLine();
        }
    }
}
