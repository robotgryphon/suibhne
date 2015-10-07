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

        public MainWindow() {
            InitializeComponent();

            // TODO: Make fancier loading icon for startup process

            Core.LoadConfiguration();
            Core.LoadNetworks();

            ExtensionSystem.Instance.Start();
            Ostenvighx.Suibhne.Commands.CommandManager.Instance.MapCommands();


            MenuItem extensionsMain = (MenuItem) this.FindName("extensionsMenu");

            foreach (ExtensionMap em in ExtensionSystem.GetExtensionList()) {
                MenuItem newExtensionItem = new MenuItem();
                newExtensionItem.Uid = "extensionMenuItem " + em.Identifier;
                newExtensionItem.Header = em.Name;

                extensionsMain.Items.Add(newExtensionItem);
            }
            // DO STARTUP AFTER DASHBOARD CREATED: Core.StartNetworks();
        }

        private void ExitApplication(object sender, RoutedEventArgs e) {
            // DoShutdown();
            Application.Current.Shutdown();
        }

        private void click_LocationsEditor(object sender, RoutedEventArgs e) {
            Windows.Locations win = new Windows.Locations();
            win.ShowDialog();
        }
    }
}
