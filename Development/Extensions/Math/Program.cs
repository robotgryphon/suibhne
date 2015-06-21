using Ostenvighx.Suibhne.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Math_Extension {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(String[] args) {
            if (args.Length > 0 && args[0] == "--install") {
                ExtensionInstaller.DumpInstallData(typeof(MathExtension));
            } else {
                MathExtension ext = new MathExtension();
                ext.Start();

                while (!ext.Connected) { Thread.Sleep(1000); }
                while (ext.Connected) {
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
