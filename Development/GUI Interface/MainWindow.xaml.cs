using Ostenvighx.Suibhne.Extensions;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Ostenvighx.Suibhne.Gui {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {

        public MainWindow() {
            InitializeComponent();

            Core.LoadConfiguration();
            Events.EventManager.Initialize();

            Core.LoadNetworks();

            
            
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

            Suibhne.Commands.CommandManager.Initialize();
            ExtensionSystem.Initialize();

            Suibhne.Commands.CommandManager.MapCommands();


            ExtensionSystem.Instance.AllExtensionsReady += () => {
                Core.StartNetworks();
            };

            RoutedCommand toggle = new RoutedCommand();
            toggle.InputGestures.Add(new KeyGesture(Key.F2));
            CommandBindings.Add(new CommandBinding(toggle, ToggleOverlay));
        }

        private void EventDebugHandler(string json, Core.Side s) {
            if (EventScrollback.Dispatcher.CheckAccess()) {
                Border b = new Border();
                b.BorderThickness = new Thickness(0, 0, 0, 1);
                b.BorderBrush = new SolidColorBrush(Colors.Bisque);
                b.Padding = new Thickness(5);

                TextBlock tb = new TextBlock();
                tb.Text = json;
                b.Child = tb;

                Color c = Colors.Red;
                switch (s) {
                    case Core.Side.INTERNAL:
                        c = Colors.RoyalBlue;
                        break;

                    case Core.Side.EXTENSION:
                        c = Colors.SeaShell;
                        break;

                    case Core.Side.CONNECTOR:
                        c = Colors.SlateGray;
                        break;
                }

                tb.Foreground = new SolidColorBrush(c);

                EventScrollback.Children.Add(b);

                EventScrollbackContainer.ScrollToBottom();
            } else
                EventScrollback.Dispatcher.Invoke(new Action(() => { EventDebugHandler(json, s); }));
        }

        private void ToggleOverlay(object sender, RoutedEventArgs e) {
            Core.Log("Overlay button pressed");

            if(OverlayContainer.Visibility == Visibility.Collapsed) {
                OverlayContainer.Visibility = Visibility.Visible;
                Events.EventManager.Instance.OnEventHandled += EventDebugHandler;
            } else {
                OverlayContainer.Visibility = Visibility.Collapsed;
                Events.EventManager.Instance.OnEventHandled -= EventDebugHandler;
            }
           
        }

        private void ExitApplication(object sender, RoutedEventArgs e) {
            // TODO: DoShutdown();
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
