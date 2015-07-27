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
            InitializeComponent();

            Core.LoadConfiguration();
            Core.LoadNetworks();

            Core.OnLogMessage += (m, t) => {
                AddOutput(m.ToString());
            };
        }

        public void AddOutput(String s) {
            if (this.output.Dispatcher.CheckAccess()) {
                this.output.AppendText(s + "\u2028");
            } else {
                this.output.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    new Action(() => {
                        this.output.AppendText(s + "\u2028");
                    })
                );
            }
        }

        public void AddOutput(Message m) {
            AddOutput(m.ToString());
        }
    }
}
