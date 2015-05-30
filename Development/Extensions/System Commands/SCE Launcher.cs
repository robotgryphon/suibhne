using Ostenvighx.Suibhne.Common;
using Ostenvighx.Suibhne.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace System_Commands {

    static class Launcher {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(String[] args) {
            Console.WriteLine("Starting with args: {0}", String.Join(" ", args));
            
            if (args.Length > 0 && args[0] == "--install") {
                Console.WriteLine("Dumping installation data...");
                ExtensionInstaller.DumpInstallData(typeof(SystemCommands), true);
            } else {
                SystemCommands sce = new SystemCommands();
                sce.Start();

                Console.Write("Starting system commands");
                while (!sce.Connected) {
                    // Startup process
                    Console.Write(".");
                    Thread.Sleep(100);
                }

                Console.WriteLine();
                while (sce.Connected) { }
            }            
        }
    }
}
