using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Ostenvighx.Suibhne {
    public class Launcher {

        [STAThread]
        public static void Main() {

            Interface_v2.MainWindow w = new Interface_v2.MainWindow();
            w.Show();

            Core.Log("Definitely started");


            Interface_v2.App a = new Interface_v2.App();
            a.Run();
        }
    }
}
