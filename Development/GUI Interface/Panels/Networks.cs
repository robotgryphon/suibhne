using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using System.Xaml;
using Nini.Config;
using System.Windows.Media.Imaging;

namespace Ostenvighx.Suibhne.Gui.Panels {
    internal class NetworkPanel : PanelBase {

        public Grid Panel;

        private StackPanel configPanel;
        private StackPanel networkList;

        public NetworkPanel() {
            this.Panel = new Grid();

            SetupSidebar();
            SetupConfigArea();
        }

        private void SetupConfigArea() {
            configPanel = new StackPanel();
            configPanel.CanVerticallyScroll = true;
            configPanel.VerticalAlignment = VerticalAlignment.Top;

            ScrollViewer sv = new ScrollViewer();
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            sv.Content = configPanel;
            sv.SetValue(Grid.ColumnProperty, 1);

            WrapPanel w = new WrapPanel();
            w.Background = new SolidColorBrush(Colors.Sienna);
            w.Height = 50;

            configPanel.Children.Add(w);

            Label message = new Label();
            message.Content = "Select a network from the left to edit, or add a new network.";
            message.HorizontalAlignment = HorizontalAlignment.Center;
            message.Margin = new Thickness(0, 10, 0, 0);
            message.FontSize = 16;
            configPanel.Children.Add(message);

            Panel.Children.Add(sv);
        }

        private void SetupSidebar() {
            ColumnDefinition sidebar = new ColumnDefinition();
            sidebar.Width = new GridLength(220);
            Panel.ColumnDefinitions.Add(sidebar);

            Panel.ColumnDefinitions.Add(new ColumnDefinition());

            #region Sidebar
            Grid sidebarContainer = new Grid();

            RowDefinition toolbar = new RowDefinition();
            toolbar.Height = new GridLength(50);
            sidebarContainer.RowDefinitions.Add(toolbar);
            sidebarContainer.RowDefinitions.Add(new RowDefinition());
            Panel.Children.Add(sidebarContainer);

            StackPanel tools = new StackPanel();
            tools.SetValue(Grid.RowProperty, 0);
            tools.Height = 50;
            tools.Background = new SolidColorBrush(Colors.Sienna);

            tools.Width = sidebarContainer.Width;
            Button addBtn = new Button();
            addBtn.Content = "+ Add New Network";
            addBtn.Margin = new Thickness(10, 4, 10, 4);
            addBtn.Height = tools.Height - 10;
            addBtn.Background = new SolidColorBrush(Colors.Transparent);
            addBtn.HorizontalAlignment = HorizontalAlignment.Stretch;
            addBtn.SetValue(Grid.ColumnProperty, 0);
            addBtn.VerticalAlignment = VerticalAlignment.Center;
            addBtn.BorderThickness = new Thickness(0);
            addBtn.Foreground = new SolidColorBrush(Colors.White);
            tools.Children.Add(addBtn);

            sidebarContainer.Children.Add(tools);

            ScrollViewer networksScroller = new ScrollViewer();
            networksScroller.SetValue(Grid.RowProperty, 1);
            networksScroller.Margin = new Thickness(0, 0, 10, 0);
            networksScroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            sidebarContainer.Children.Add(networksScroller);

            this.networkList = new StackPanel();
            networksScroller.Content = networkList;
            networkList.Margin = new Thickness(0, 10, 0, 0);
            foreach (NetworkBot b in Core.Networks.Values) {
                Button l = new Button();
                l.Content = b.FriendlyName;
                l.HorizontalAlignment = HorizontalAlignment.Stretch;
                l.Padding = new Thickness(2);
                l.Margin = new Thickness(10, 0, 0, 2);
                l.Background = new SolidColorBrush(Colors.Transparent);
                l.BorderThickness = new Thickness(0);
                l.Name = b.Identifier.ToString().Replace("-", "_");
                l.Click += NetworkButtonClick;

                networkList.Children.Add(l);
            }
            #endregion
        }

        private void LoadNetworkConfig(NetworkBot b, IniConfigSource config, String filename) {

            configPanel.Children.Clear();

            Grid stackHeaderContainer = new Grid();
            stackHeaderContainer.ColumnDefinitions.Add(new ColumnDefinition());

            ColumnDefinition toolbarHeaderContainer = new ColumnDefinition();
            toolbarHeaderContainer.Width = new GridLength(120);
            stackHeaderContainer.ColumnDefinitions.Add(toolbarHeaderContainer);

            configPanel.Children.Add(stackHeaderContainer);
            
            Label stackHeader = new Label();
            stackHeader.SetValue(Grid.ColumnProperty, 0);
            stackHeader.FontSize = 30;
            stackHeader.Content = "Editing Network: " + b.FriendlyName;
            stackHeader.Foreground = new SolidColorBrush(Colors.White);
            stackHeader.Background = new SolidColorBrush(Colors.Sienna);
            stackHeader.Height = 50;
            stackHeaderContainer.Children.Add(stackHeader);

            WrapPanel networkOptionsPanel = new WrapPanel();
            networkOptionsPanel.SetValue(Grid.ColumnProperty, 1);
            networkOptionsPanel.Background = new SolidColorBrush(Colors.Sienna);
            stackHeaderContainer.Children.Add(networkOptionsPanel);

            Button saveNetworkBtn = new Button();
            saveNetworkBtn.Width = 40;
            saveNetworkBtn.Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/GUI Interface;component/Images/save.png", UriKind.Absolute)));
            saveNetworkBtn.Height = 40;
            saveNetworkBtn.Margin = new Thickness(0, 5, 0, 5);

            networkOptionsPanel.Children.Add(saveNetworkBtn);


            Button delNetworkBtn = new Button();
            delNetworkBtn.Width = 40;
            delNetworkBtn.Height = 40;
            delNetworkBtn.Background = new ImageBrush(new BitmapImage(new Uri("pack://application:,,,/GUI Interface;component/Images/trash.png", UriKind.Absolute)));
            delNetworkBtn.BorderThickness = new Thickness(0);
            delNetworkBtn.Margin = new Thickness(5);

            networkOptionsPanel.Children.Add(delNetworkBtn);

            try {
                String stackText = File.ReadAllText(filename);
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(stackText);

                // Load in all the nodes to generate the config editor
                foreach (XmlNode node in xml.ChildNodes[1].ChildNodes) {
                    ParseNode(configPanel, config, node);
                }
            }

            catch (Exception e) {
                Console.WriteLine(e);
            }

            
        }

        public override Panel GetPanel() {
            return Panel;
        }

        void NetworkButtonClick(object sender, RoutedEventArgs e) {
            // Clear network panel
            // Get network details
            // Load in network config file for network type
            // Load in existing config data
            Button networkButton = (Button) sender;

            foreach (object o in networkList.Children) {
                Button netBtn = (Button)o;
                netBtn.Foreground = new SolidColorBrush(Colors.Black);
                netBtn.Background = new SolidColorBrush(Colors.Transparent);
            }

            networkButton.Background = new SolidColorBrush(Colors.SlateGray);
            networkButton.Foreground = new SolidColorBrush(Colors.White);

            NetworkBot b = Core.Networks[Guid.Parse(networkButton.Name.Replace("_", "-"))];

            IniConfigSource config = new IniConfigSource(Core.ConfigDirectory + @"Networks\" + b.FriendlyName + @"\" + b.FriendlyName + ".ini");

            String networkType = config.Configs["Network"].GetString("Type");

            if (networkType != null && networkType != "") {
                String netConfigTemplateFilename = Core.ConfigDirectory + @"NetworkTypes\" + networkType + ".xml";
                LoadNetworkConfig(b, config, netConfigTemplateFilename);
            }
        }

        private void ParseNode(StackPanel configPanel, IniConfigSource config, XmlNode n) {
            switch (n.Name.ToLower()) {
                case "field":
                    WrapPanel fieldPanel = new WrapPanel();
                    if (n.Attributes["Type"] == null) return;

                    String sectionName = n.ParentNode.Attributes["Name"].Value;
                    IConfig section = config.Configs[sectionName];

                    if (n.Attributes["Label"] != null) {
                        Label fieldLabel = new Label();
                        fieldLabel.Content = n.Attributes["Label"];
                        fieldLabel.Width = 120;
                        fieldPanel.Children.Add(fieldLabel);
                    }

                    switch (n.Attributes["Type"].Value.ToLower()) {
                        case "text":
                            TextBox tb = new TextBox();
                            tb.Height = 20;
                            tb.HorizontalAlignment = HorizontalAlignment.Stretch;
                            tb.MinWidth = 400;
                            tb.Name = n.Attributes["Key"].Value;

                            if (section.Contains(n.Attributes["Key"].Value)) {
                                tb.Text = section.GetString(n.Attributes["Key"].Value);
                            }

                            fieldPanel.Children.Add(tb);
                            break;

                        case "checkbox":
                            CheckBox cb = new CheckBox();
                            cb.Margin = new Thickness(0, 5, 0, 5);
                            cb.Name = n.Attributes["Key"].Value;
                            fieldPanel.Children.Add(cb);

                            if (section.Contains(n.Attributes["Key"].Value)) {
                                cb.IsChecked = section.GetBoolean(n.Attributes["Key"].Value);
                            }
                            break;

                        case "password":
                            PasswordBox p = new PasswordBox();
                            p.Height = 20;
                            p.HorizontalAlignment = HorizontalAlignment.Stretch;
                            p.MinWidth = 400;
                            p.Name = n.Attributes["Key"].Value;
                            fieldPanel.Children.Add(p);

                            if (section.Contains(n.Attributes["Key"].Value)) {
                                p.Password = section.GetString(n.Attributes["Key"].Value);
                            }
                            break;

                        case "select":
                            ComboBox select = new ComboBox();
                            select.Width = 400;
                            select.Name = n.Attributes["Key"].Value;
                            foreach (XmlNode childNode in n.ChildNodes) {
                                ComboBoxItem cbi = new ComboBoxItem();
                                cbi.Content = childNode.InnerText;
                                
                                select.Items.Add(cbi);
                            }

                            fieldPanel.Children.Add(select);
                            break;

                        default:
                            // Unsupported type
                            break;

                    }
                    configPanel.Children.Add(fieldPanel);
                    break;

                case "section":
                    Label l = new Label();
                    l.Content = n.Attributes["Name"].Value;
                    l.FontSize = 20;
                    l.Margin = new Thickness(0, 20, 40, 6);
                    l.BorderThickness = new Thickness(0, 0, 0, 1);
                    l.BorderBrush = new SolidColorBrush(Colors.SlateBlue);
                    configPanel.Children.Add(l);

                    foreach (XmlNode childNode in n.ChildNodes) {
                        ParseNode(configPanel, config, childNode);
                    }
                    break;

                case "text":
                    Label b = new Label();
                    if (n.InnerText != "")
                        b.Content = n.InnerText;

                    if (n.Attributes["Bold"] != null)
                        b.SetValue(Label.FontWeightProperty, FontWeights.Bold);

                    configPanel.Children.Add(b);
                    break;

                default:
                    Console.WriteLine("Unsupported node type: " + n.Name);
                    break;
            }
        }
    }
}
