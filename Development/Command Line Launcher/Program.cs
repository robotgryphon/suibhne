using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ostenvighx.Suibhne;
using Ostenvighx.Suibhne.Extensions;
using Ostenvighx.Suibhne.Events;
using Ostenvighx.Suibhne.Commands;

namespace CLI {
    class Program {
        static void Main(string[] args) {

            Core.Initialize();

            ExtensionSystem.Instance.AllExtensionsReady += () => {
                Core.Start();
            };

            Console.ReadLine();
        }
    }
}
