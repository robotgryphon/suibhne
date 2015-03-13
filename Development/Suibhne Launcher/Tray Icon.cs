using Suibhne_Launcher.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Raindrop.Suibhne {
    class SuibhneIcon : ApplicationContext {

        private NotifyIcon trayIcon;
        private IrcBot bot;

        public SuibhneIcon(IrcBot bot) {
            // Initialize Tray Icon
            trayIcon = new NotifyIcon()
            {
                Icon = Resources.IconTray,
                ContextMenu = new ContextMenu(new MenuItem[] { new MenuItem("Exit", Exit) }),
                Visible = true
            };

            trayIcon.Text = "Suibhne IRC System";
            this.bot = bot;
        }

        void Exit(object sender, EventArgs e) {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            trayIcon.Visible = false;
            bot.Disconnect();

            Application.Exit();
        }
    }
}
