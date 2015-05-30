using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Ostenvighx.Suibhne;
using Ostenvighx.Suibhne.Extensions;
using Nini.Config;
using System.IO;

namespace Graphical_Interface {
    public partial class MainWindow : Form {
        public MainWindow() {
            InitializeComponent();

            ExtensionSystem registry = new ExtensionSystem(Environment.CurrentDirectory + "/extensions.ini");

            try {
                IniConfigSource systemConfig = new IniConfigSource(Environment.CurrentDirectory + "/suibhne.ini");

                String serverRootDirectory = systemConfig.Configs["Suibhne"].GetString("ServerRootDirectory", Environment.CurrentDirectory + "/Configuration/Servers/");
                String[] serverDirectories = Directory.GetDirectories(serverRootDirectory);

                foreach (String serverDirectory in serverDirectories) {
                    IrcBot server = new IrcBot(serverDirectory, registry);

                    NetworkBlock newBlock = new NetworkBlock();
                    newBlock.NetworkNameLabel.Text = server.Server.locationName;

                    networksPanel.Controls.Add(newBlock);
                    networksPanel.Refresh();

                    server.Connect();
                }

            }

            catch (FileNotFoundException fnfe) {
                Console.WriteLine("Server configuration file not found: " + fnfe.Message);
            }

            catch (Exception e) {
                Console.WriteLine("Exception thrown: " + e);
            }

        }

        private void button1_Click(object sender, EventArgs e) {

        }

        private void panel1_Paint(object sender, PaintEventArgs e) {

        }

        private void closeBtn_Click(object sender, EventArgs e) {
            this.Close();
        }

        private void panelLabel_Click(object sender, EventArgs e) {

        }

        private void titleBar_Paint(object sender, PaintEventArgs e) {
            titleBar.BackColor = Color.FromArgb(200, Color.Black);
        }

        private void flowLayoutPanel1_Paint(object sender, PaintEventArgs e) {

        }
    }
}
