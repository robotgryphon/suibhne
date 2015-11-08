using Ostenvighx.Suibhne;
using Ostenvighx.Suibhne.Extensions;
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


            

            foreach (ExtensionMap em in ExtensionSystem.GetExtensionList()) {
                Grid ExtensionsPanel = new Grid();
                ExtensionsPanel.Height = 30; ExtensionsPanel.Width = 150;
                ExtensionsPanel.Background = new SolidColorBrush(Colors.GhostWhite);
                ExtensionsPanel.Margin = new Thickness(5);

                RowDefinition panelLabel = new RowDefinition();
                panelLabel.Height = new GridLength(30); 
                
                ExtensionsPanel.RowDefinitions.Add(panelLabel);

                Button label = new Button();
                label.BorderBrush = new SolidColorBrush(Colors.Black);
                label.BorderThickness = new Thickness(1);
                label.Content = em.Name;
                label.SetValue(Grid.RowProperty, 1);
                ExtensionsPanel.Children.Add(label);

                extensionsContainer.Children.Add(ExtensionsPanel);
            }
            // DO STARTUP AFTER DASHBOARD CREATED: Core.StartNetworks();
        }

        private void ExitApplication(object sender, RoutedEventArgs e) {
            // DoShutdown();
            Application.Current.Shutdown();
        }

        private void click_LocationsEditor(object sender, RoutedEventArgs e) {
            Wins.Locations win = new Wins.Locations();
            win.ShowDialog();
        }

        private void click_NewLocation(object sender, RoutedEventArgs e) {
            Wins.New_Location win = new Wins.New_Location();
            win.ShowDialog();
        }
    }
}
