using Raindrop.Suibhne.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Suibhne_Launcher {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            IrcBot bot = new IrcBot();
            bot.LoadServers();
            bot.Start();

            Application.Run(new SuibhneIcon(bot));
        }
    }
}
