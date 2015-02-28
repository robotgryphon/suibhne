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
            dice.Prepare();

            while (true) {
                // Do nothing
                Thread.Sleep(5000);
            }
        }
    }
}
