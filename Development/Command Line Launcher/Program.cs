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
            if (true || Core.RequiresUpdate) {
                String[] events = Management.GetAvailableEventsFromConnectors();
                Core.Log("Loaded all events: " + String.Join("; ", events), LogType.EXTENSIONS);
            }

            // Core.LoadNetworks();


            Console.ReadLine();
        }
    }
}
