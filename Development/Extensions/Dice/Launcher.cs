using Ostenvighx.Suibhne.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.Dice {
    
    public static class Launcher {

        [STAThread]
        static void Main(String[] args) {
            if (args.Length > 0 && args[0] == "--install") {
                ExtensionInstaller.DumpInstallData(typeof(DiceExtension));
            } else {
                DiceExtension dice = new DiceExtension();
                dice.Start();

                while (!dice.Connected) {
                    Thread.Sleep(1000); // Wait until connection is made
                }

                // Now that connection is done, keep alive
                while (dice.Connected) {
                    // Do nothing
                    Thread.Sleep(5000);
                }
            }
        }
    }
}
