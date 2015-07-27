using Ostenvighx.Suibhne;
using Ostenvighx.Suibhne.Gui.Panels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

namespace Ostenvighx.Suibhne.Gui {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();

            Core.LoadConfiguration();
            Core.LoadNetworks();

            NetworkPanel cb = new NetworkPanel();
            Grid p = cb.GetPanel();
            p.SetValue(Grid.RowProperty, 1);

            this.WindowContainer.Children.Add(p);
        }
    }
}
