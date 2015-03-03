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
        static void Main() {
            MathExtension ext = new MathExtension();
            while (!ext.Connected) { Thread.Sleep(1000); }
            while (ext.Connected) {
                Thread.Sleep(1000);
            }
        }
    }
}
