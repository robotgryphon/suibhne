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

        public Button ActiveTab;

        public MainWindow() {
            InitializeComponent();

            Core.LoadConfiguration();
            Core.LoadNetworks();

            ActiveTab = OutputTab;
            ActiveTab.Style = (Style) FindResource("TabButtonActive");

            PanelBase cb = new OutputPanel();
            Grid p = (Grid) cb.GetPanel();

            this.ContentArea.Children.Add(p);
        }

        public void HandleTabSwitch(object sender, RoutedEventArgs e) {
            Button s = (Button)sender;

            ActiveTab.Style = (Style) FindResource("TabButton");
            ActiveTab = s;
            ActiveTab.Style = (Style)FindResource("TabButtonActive");

            ContentArea.Children.Clear();

            Panel p = new StackPanel();
            switch (ActiveTab.Name.ToLower()) {
                case "networktab":
                    NetworkPanel cb = new NetworkPanel();
                    p = cb.GetPanel();
                    break;

                case "abouttab":
                    AboutPanel a = new AboutPanel();
                    p = a.GetPanel();
                    break;
            }

            this.ContentArea.Children.Add(p);            
        }
    }
}
