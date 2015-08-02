using Nini.Config;
using Ostenvighx.Suibhne.Networks.Base;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Xml;

namespace Ostenvighx.Suibhne.Gui.Panels {
    internal class NetworkPanel : PanelBase {

        public Grid Panel;

        private Guid currentNetwork;
        private String networkType;

        private StackPanel configPanel;
        private TreeView networkList;

        private Dictionary<String, FrameworkElement> Fields;

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
            addBtn.Click += Toolbar_AddNetwork;

            tools.Children.Add(addBtn);

            Button remBtn = new Button();
            remBtn.Content = "\u2212";
            remBtn.FontSize = 20;
            remBtn.Style = ToolBarStyle;
            tools.Children.Add(remBtn);

            Button saveBtn = new Button();
            saveBtn.Content = "\u2713";
            saveBtn.Style = ToolBarStyle;
            saveBtn.Click += Toolbar_SaveNetwork;
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

                Location networkLocation = LocationManager.GetLocationInfo(b.Identifier).Value;

                networkListItem.Header =  networkLocation.Name;
                networkListItem.Padding = new Thickness(2);
                networkListItem.Margin = new Thickness(10, 0, 0, 2);
                networkListItem.FontWeight = FontWeights.Bold;
                networkListItem.Uid = b.Identifier.ToString();
                networkListItem.Selected += this.LocationList_ClickNetworkItem;

                DataTable knownChildren = LocationManager.GetChildLocations(b.Identifier);
                foreach (DataRow l in knownChildren.Rows) {
                    TreeViewItem locationItem = new TreeViewItem();
                    locationItem.Header = l["Name"].ToString();
                    locationItem.Uid = l["Identifier"].ToString();
                    locationItem.FontWeight = FontWeights.Normal;
                    locationItem.Selected += this.LocationList_ClickLocationItem;

                    networkListItem.Items.Add(locationItem);
                }

                networkList.Items.Add(networkListItem);
            }
            #endregion
        }

        private void Toolbar_AddNetwork(object sender, RoutedEventArgs e) {
            currentNetwork = Guid.Empty;
            
            string[] networkTypes = Directory.GetFiles(Core.ConfigDirectory + "/NetworkTypes", "*.xml");

            Window chooser = new Window();
            chooser.Title = "Choose Network Type";

            StackPanel chooserDialogContainer = new StackPanel();
            chooserDialogContainer.Margin = new Thickness(10);

            Label help = new Label();
            help.Content = "Choose a network type:";

            chooserDialogContainer.Children.Add(help);

            ComboBox typechooser = new ComboBox();

            Button confirm = new Button();
            confirm.Margin = new Thickness(0, 10, 0, 0);
            confirm.Content = "Add Network";
            confirm.Background = new SolidColorBrush(Colors.Transparent);
            confirm.BorderBrush = new SolidColorBrush(Colors.Black);
            confirm.BorderThickness = new Thickness(1);
            confirm.Click += (A, B) => {
                if (typechooser.SelectedItem == null)
                    MessageBox.Show("You need to select a network type to continue.");
                else
                    chooser.Close();
            };


            foreach (String networkType in networkTypes) {
                typechooser.Items.Add(new FileInfo(networkType).Name.Split('.')[0]);
            }

            chooserDialogContainer.Children.Add(typechooser);
            chooserDialogContainer.Children.Add(confirm);

            chooser.Content = chooserDialogContainer;
            chooser.Width = 400;
            chooser.Height = 145;

            chooser.ShowDialog();

            // If they didn't select an item, cancel
            if (typechooser.SelectedItem == null)
                return;

            this.networkType = typechooser.SelectedItem.ToString();

            IniConfigSource ini = new IniConfigSource();

            configPanel.Children.Clear();

            SetupEditorHeader("New Network");

            LoadNetworkFields(this.networkType, ini);
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

        private void Toolbar_SaveNetwork(object sender, RoutedEventArgs e) {

            // Figure out if location or network selected
            TreeViewItem selected = (TreeViewItem) networkList.SelectedItem;

            Location lData = LocationManager.GetLocationInfo(Guid.Parse(selected.Uid)).Value;
            switch (lData.Type) {
                case Reference.LocationType.Network:
                    if (currentNetwork != Guid.Empty) {
                        NetworkBot network = Core.Networks[currentNetwork];

                        // First, disconnect network to make sure settings are reloaded properly
                        network.Disconnect();

                        // Then, save the network settings for that network
                        SaveNetworkSettings(network.Identifier);


                        // After that, reload the network's settings fromt he net object
                        network.ReloadConfiguration();

                        // Reconnect to network again.
                        network.Connect();
                    } else {

                        // Saving NEW network item
                        SaveNetworkSettings(Guid.NewGuid());
                    }
                    break;

                case Reference.LocationType.Public:
                    MessageBox.Show("Location editing not yet implemented");
                    break;

                default:
                    // Not handled
                    break;
            }

            


        }

        private void SaveNetworkSettings(Guid id) {

            Boolean newNetwork = false;

            if (id == currentNetwork) {
                // Saving over file

                MessageBoxResult res = MessageBox.Show("Warning: By confirming this save, you're letting the network configuration " + 
                    "file be rewritten in its entirety by the data you specified here. Any custom content WILL be removed. Press okay " + 
                    "to confirm." + "\n\n" + 
                    "If you'd like to make notes, feel free to create new files of your own in the network folder. They won't be removed unless YOU do it.",

                    "Confirm config file overwrite?",
                    MessageBoxButton.OKCancel);

                if (res != MessageBoxResult.OK)
                    return;
            } else {

                id = Guid.NewGuid();

                // Insert into database
                LocationManager.AddNewNetwork(id, "Unnamed Network");

                // Create directories
                Directory.CreateDirectory(Core.ConfigDirectory + "/Networks/" + id);
                Directory.CreateDirectory(Core.ConfigDirectory + "/Networks/" + id + "/Locations");

                newNetwork = true;
            }

            IniConfigSource networkConfig = new IniConfigSource();
            networkConfig.Save( Core.ConfigDirectory + "/Networks/" + id + "/network.ini" );

            // Add in a little warning for the people that like to hand-edit stuff
            String[] fileOverwriteComment = new string[]{
                        "# Warning: This is an automatically generated configuration file. All edits will not be saved here when the editor is used.",
                        "# To avoid loss of notes, create a different file in this directory with the notes.",
                        "",
                        ""
                    };

            File.WriteAllLines(networkConfig.SavePath, fileOverwriteComment);

            // Okay. Now, let's get to saving stuff.
            // First, open that structure file back up.
            String stackText = File.ReadAllText(Core.ConfigDirectory + "/NetworkTypes/" + this.networkType + ".xml");
            XmlDocument structure = new XmlDocument();
            structure.LoadXml(stackText);

            #region Save Network Config File
            networkConfig.AddConfig("Network");
            networkConfig.Configs["Network"].Set("type", this.networkType);

            foreach (XmlNode section in structure.ChildNodes[1].ChildNodes) {
                String sectionName = section.Attributes["Name"].Value;
                networkConfig.AddConfig(sectionName);

                foreach (XmlNode field in section.ChildNodes) {
                    if (field.Attributes["Type"] != null && field.Name.ToLower() == "field") {
                        String fieldKey = field.Attributes["Key"].Value;
                        switch (field.Attributes["Type"].Value.ToLower()) {
                            case "text":
                                networkConfig.Configs[sectionName].Set(fieldKey, ((TextBox)Fields[fieldKey]).Text);
                                break;

                            case "password":
                                networkConfig.Configs[sectionName].Set(fieldKey, ((PasswordBox)Fields[fieldKey]).Password);
                                break;

                            case "select":
                                networkConfig.Configs[sectionName].Set(fieldKey, ((ComboBox)Fields[fieldKey]).SelectedValue.ToString());
                                break;
                        }
                    }
                }

            }

            networkConfig.Save();
            #endregion

            if (newNetwork) {
                TreeViewItem networkListItem = new TreeViewItem();
                networkListItem.Header = "New Network";
                networkListItem.Padding = new Thickness(2);
                networkListItem.Margin = new Thickness(10, 0, 0, 2);
                networkListItem.FontWeight = FontWeights.Bold;
                networkListItem.Uid = id.ToString();
                networkListItem.IsSelected = true;

                networkListItem.Selected += this.LocationList_ClickNetworkItem;

                networkList.Items.Add(networkListItem);

            }
        }

        public override Panel GetPanel() {
            return Panel;
        }
        
        /// <summary>
        /// Handles a click on a location item in the location list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LocationList_ClickLocationItem(object sender, RoutedEventArgs e) {
            TreeViewItem selectedLocation = (TreeViewItem)sender;

            configPanel.Children.Clear();
            SetupEditorHeader("Editing Location: " + selectedLocation.Header);


            Guid locationID = Guid.Parse(selectedLocation.Uid);
            Location locationInfo = LocationManager.GetLocationInfo(locationID).Value;
            if (locationInfo == null || locationInfo.Parent == Guid.Empty)
                return;

            TextBlock infoBlock = new TextBlock();
            infoBlock.Margin = new Thickness(0, 8, 0, 0);

            infoBlock.Inlines.Add(new Bold(new Run("Location Identifier: ")));
            infoBlock.Inlines.Add(locationID.ToString());

            configPanel.Children.Add(infoBlock);

            // TODO: Add location fields

            e.Handled = true;
        }

        void LoadNetworkFields(string networkType, IniConfigSource config) {
            String netConfigTemplateFilename = Core.ConfigDirectory + @"NetworkTypes\" + networkType + ".xml";

            try {
                String stackText = File.ReadAllText(netConfigTemplateFilename);
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(stackText);

                this.Fields = new Dictionary<string, FrameworkElement>();

                // Load in all the nodes to generate the config editor
                foreach (XmlNode node in xml.ChildNodes[1].ChildNodes) {
                    ParseConfigStructure(configPanel, config, node);
                }
            }

            catch (Exception netEx) {
                Console.WriteLine(netEx);
            }
        }

        /// <summary>
        /// Handles when a network item is selected from the location list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void LocationList_ClickNetworkItem(object sender, RoutedEventArgs e) {
            // Clear network panel
            // Get network details
            // Load in network config file for network type
            // Load in existing config data
            TreeViewItem networkButton = (TreeViewItem)sender;

            NetworkBot b = Core.Networks[Guid.Parse(networkButton.Uid)];
            currentNetwork = b.Identifier;

            Location networkAsLocation = LocationManager.GetLocationInfo(b.Identifier).Value;

            IniConfigSource config = new IniConfigSource(Core.ConfigDirectory + @"Networks\" + b.Identifier + @"\network.ini");
            config.CaseSensitive = false;

            this.networkType = config.Configs["Network"].GetString("type");

            if (networkType != null && networkType != "") {
                

                configPanel.Children.Clear();

                SetupEditorHeader("Editing Network: " + networkAsLocation.Name);

                TextBlock networkInfo = new TextBlock();
                networkInfo.Margin = new Thickness(10, 8, 0, 0);

                networkInfo.Inlines.Add(new Bold(new Run("Network Identifier: ")));
                networkInfo.Inlines.Add(b.Identifier.ToString() + " (");
                networkInfo.Inlines.Add(new Italic(new Run("Last modified on ")));
                networkInfo.Inlines.Add(File.GetLastWriteTime(Core.ConfigDirectory + "/Networks/" + b.Identifier + "/network.ini") + ")");

                configPanel.Children.Add(networkInfo);

                LoadNetworkFields(networkType, config);
            }
        }

        /// <summary>
        /// Parses a single node in a configuration structure file. This is for network and ocation files.
        /// </summary>
        /// <param name="configPanel"></param>
        /// <param name="config"></param>
        /// <param name="n"></param>
        private void ParseConfigStructure(StackPanel configPanel, IniConfigSource config, XmlNode n) {
            switch (n.Name.ToLower()) {
                case "field":
                    WrapPanel fieldPanel = new WrapPanel();
                    if (n.Attributes["Type"] == null) return;

                    String sectionName = n.ParentNode.Attributes["Name"].Value;
                    IConfig section = null;
                    if(config != null) section = config.Configs[sectionName];

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

                            this.Fields.Add(tb.Name, tb);

                            if (config != null && section != null && section.Contains(n.Attributes["Key"].Value)) {
                                tb.Text = section.GetString(n.Attributes["Key"].Value);
                            }

                            fieldPanel.Children.Add(tb);
                            break;

                        case "checkbox":
                            CheckBox cb = new CheckBox();
                            cb.Margin = new Thickness(0, 5, 0, 5);
                            cb.Name = n.Attributes["Key"].Value;
                            fieldPanel.Children.Add(cb);
                            this.Fields.Add(cb.Name, cb);

                            if (config != null && section != null && section.Contains(n.Attributes["Key"].Value)) {
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
                            this.Fields.Add(p.Name, p);

                            if (config != null && section != null && section.Contains(n.Attributes["Key"].Value)) {
                                p.Password = section.GetString(n.Attributes["Key"].Value);
                            }
                            break;

                        case "select":
                            ComboBox select = new ComboBox();
                            select.Width = 400;
                            select.Name = n.Attributes["Key"].Value;
                            this.Fields.Add(select.Name, select);

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
                        ParseConfigStructure(configPanel, config, childNode);
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
