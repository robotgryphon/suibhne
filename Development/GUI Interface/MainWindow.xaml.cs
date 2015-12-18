using Newtonsoft.Json.Linq;
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

            ExtensionSystem.Instance.AllExtensionsReady += () => {
                Core.Start();
            };

            Core.Initialize();


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
            Wins.ConnectionList win = new Wins.ConnectionList();
            win.ShowDialog();
        }

        private void click_NewLocation(object sender, RoutedEventArgs e) {
            // Wins.New_Location win = new Wins.New_Location();
            // win.ShowDialog();
        }

        private void InjectEventHandler(object sender, RoutedEventArgs e) {
            String event_text = EventInjectionEntry.Text;
            JObject test = new JObject();
            try { test = JObject.Parse(event_text); }
            catch (Exception) { MessageBox.Show("Invalid json to inject into event system. Please verify the syntax is correct.");  }

            try { Events.EventManager.HandleInternalEvent(test); }
            catch(Exception ex) { }
        }
    }
}
