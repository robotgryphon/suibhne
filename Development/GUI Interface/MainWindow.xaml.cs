using Ostenvighx.Suibhne;
using Ostenvighx.Suibhne.Extensions;
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
        private OutputPanel Output;
        private NetworkPanel Editor;
        private ExtensionsPanel Extensions;
        // private ScriptEditor Scripting;
        private AboutPanel About;

        public MainWindow() {
            InitializeComponent();

            Core.LoadConfiguration();
            Core.LoadNetworks();

            ExtensionSystem.Instance.Start();
            Ostenvighx.Suibhne.Commands.CommandManager.Instance.MapCommands();

            ActiveTab = OutputTab;
            ActiveTab.Style = (Style) FindResource("TabButtonActive");

            Output = new OutputPanel();
            Editor = new NetworkPanel();
            About = new AboutPanel();
            Extensions = new ExtensionsPanel();

            Panel p = Output.GetPanel();

            this.ContentArea.Children.Add(p);

            Core.StartNetworks();
        }

        public void HandleTabSwitch(object sender, RoutedEventArgs e) {
            Button s = (Button)sender;

            ActiveTab.Style = (Style) FindResource("TabButton");
            ActiveTab = s;
            ActiveTab.Style = (Style)FindResource("TabButtonActive");

            ContentArea.Children.Clear();

            Panel p = new StackPanel();
            switch (ActiveTab.Name.ToLower()) {
                case "outputtab":
                    p = Output.GetPanel();
                    break;

                case "networktab":
                    p = Editor.GetPanel();
                    break;

                case "extensionstab":
                    p = Extensions.GetPanel();
                    break;

                case "abouttab":
                    p = About.GetPanel();
                    break;
            }

            this.ContentArea.Children.Add(p);            
        }
    }
}
