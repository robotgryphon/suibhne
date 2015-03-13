using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Raindrop.Suibhne.Dice {
    
    public static class Launcher {

        [STAThread]
        static void Main() {
            DiceExtension dice = new DiceExtension();
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
