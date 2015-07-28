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
using Ostenvighx.Suibhne.Networks.Base;
using System.Windows.Documents;
using System.Data;

namespace Ostenvighx.Suibhne.Gui.Panels {
    internal class NetworkPanel : PanelBase {

        public Grid Panel;

        private Guid currentNetwork;

        private StackPanel configPanel;
        private TreeView networkList;

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
            tools.Orientation = Orientation.Horizontal;
            tools.SetValue(Grid.RowProperty, 0);
            tools.Height = 50;
            tools.Background = new SolidColorBrush(Colors.Sienna);
            tools.HorizontalAlignment = HorizontalAlignment.Stretch;
            tools.Width = 220;

            Style ToolBarStyle = (Style)Application.Current.FindResource("PanelActionButton");

            tools.Width = sidebarContainer.Width;
            Button addBtn = new Button();
            addBtn.Content = "\u002B";
            addBtn.FontSize = 20;

            // Override default style to center toolbar buttons
            addBtn.Margin = new Thickness(22.5, 5, 2.5, 5);
            addBtn.Style = ToolBarStyle;


            tools.Children.Add(addBtn);

            Button remBtn = new Button();
            remBtn.Content = "\u2212";
            remBtn.FontSize = 20;
            remBtn.Style = ToolBarStyle;
            tools.Children.Add(remBtn);

            Button saveBtn = new Button();
            saveBtn.Content = "\u2713";
            saveBtn.Style = ToolBarStyle;
            tools.Children.Add(saveBtn);

            sidebarContainer.Children.Add(tools);

            ScrollViewer networksScroller = new ScrollViewer();
            networksScroller.SetValue(Grid.RowProperty, 1);
            networksScroller.Margin = new Thickness(0, 0, 10, 0);
            networksScroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            sidebarContainer.Children.Add(networksScroller);


            this.networkList = new TreeView();
            networkList.BorderThickness = new Thickness(0);
            networksScroller.Content = networkList;
            networkList.Margin = new Thickness(0, 10, 0, 0);
            foreach (NetworkBot b in Core.Networks.Values) {
                TreeViewItem networkListItem = new TreeViewItem();
                networkListItem.Header = b.FriendlyName;
                networkListItem.HorizontalAlignment = HorizontalAlignment.Stretch;
                networkListItem.Padding = new Thickness(2);
                networkListItem.Margin = new Thickness(10, 0, 0, 2);
                networkListItem.Background = new SolidColorBrush(Colors.Transparent);
                networkListItem.BorderThickness = new Thickness(0);
                networkListItem.FontWeight = FontWeights.Bold;
                networkListItem.Uid = b.Identifier.ToString();
                networkListItem.Selected += this.NetworkButtonClick;

                Dictionary<Guid, Location> knownLocations = b.GetKnownLocations();
                foreach (KeyValuePair<Guid, Location> l in knownLocations) {
                    TreeViewItem locationItem = new TreeViewItem();
                    locationItem.Header = l.Value.Name;
                    locationItem.Uid = l.Key.ToString();
                    locationItem.FontWeight = FontWeights.Normal;
                    locationItem.Selected += this.LocationItemClick;

                    networkListItem.Items.Add(locationItem);
                }

                networkList.Items.Add(networkListItem);
            }
            #endregion
        }

        void LocationItemClick(object sender, RoutedEventArgs e) {
            TreeViewItem selectedLocation = (TreeViewItem)sender;

            Guid locationID = Guid.Parse(selectedLocation.Uid);
            Location locationInfo = Utilities.GetLocationInfo(locationID).Value;
            if (locationInfo.Parent == Guid.Empty)
                return;

            NetworkBot bot = Core.Networks[locationInfo.Parent];
            Dictionary<Guid, Location> known = bot.GetKnownLocations();

            Location location = known[locationID];

            configPanel.Children.Clear();

            SetupEditorHeader("Editing Location: " + location.Name);

            TextBlock networkInfo = new TextBlock();
            networkInfo.Margin = new Thickness(0, 8, 0, 0);

            networkInfo.Inlines.Add(new Bold(new Run("Location Identifier: ")));
            networkInfo.Inlines.Add(locationID.ToString());

            configPanel.Children.Add(networkInfo);

            e.Handled = true;
        }

        private void SetupEditorHeader(string headerText) {
            Label stackHeader = new Label();
            stackHeader.Margin = new Thickness(0);
            stackHeader.FontSize = 30;
            stackHeader.Content = headerText;
            stackHeader.Foreground = new SolidColorBrush(Colors.White);
            stackHeader.Background = new SolidColorBrush(Colors.Sienna);
            stackHeader.Height = 50;

            configPanel.Children.Add(stackHeader);
        }

        private void LoadNetworkConfig(NetworkBot b, IniConfigSource config, String filename) {

            configPanel.Children.Clear();

            SetupEditorHeader("Editing Network: " + b.FriendlyName);

            TextBlock networkInfo = new TextBlock();
            networkInfo.Margin = new Thickness(10, 8, 0, 0);

            networkInfo.Inlines.Add(new Bold(new Run("Network Identifier: ")));
            networkInfo.Inlines.Add(b.Identifier.ToString() + " (");
            networkInfo.Inlines.Add(new Italic(new Run("Last modified on ")));
            networkInfo.Inlines.Add(File.GetLastWriteTime(Core.ConfigDirectory + "/Networks/" + b.FriendlyName + "/" + b.FriendlyName + ".ini") + ")");

            configPanel.Children.Add(networkInfo);

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

        private void SaveNetworkConfig(object sender, RoutedEventArgs e) {
            NetworkBot network = Core.Networks[currentNetwork];

            network.Disconnect();


        }

        public override Panel GetPanel() {
            return Panel;
        }

        void NetworkButtonClick(object sender, RoutedEventArgs e) {
            // Clear network panel
            // Get network details
            // Load in network config file for network type
            // Load in existing config data
            TreeViewItem networkButton = (TreeViewItem)sender;

            NetworkBot b = Core.Networks[Guid.Parse(networkButton.Uid)];
            currentNetwork = b.Identifier;

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
