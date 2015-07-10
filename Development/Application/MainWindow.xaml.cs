using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Drawing;
using Ostenvighx.Suibhne;
using Ostenvighx.Suibhne.Networks.Base;

namespace Interface_v2 {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            Core.DoStartup();

            InitializeComponent();

            foreach (NetworkBot bot in Core.Networks.Values) {
                bot.Network.OnMessageRecieved += AddOutput;
            }
        }

        private void AddOutput(Message m) {
            if (this.output.Dispatcher.CheckAccess()) {
                this.output.AppendText(m.ToString() + "\u2028");
            } else {
                this.output.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(() => {
                        this.output.AppendText(m.ToString() + "\u2028");           
                    })
                );
            }
        }
    }
}
