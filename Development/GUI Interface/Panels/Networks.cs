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

        private StackPanel configPanel;
        private Guid currentItem;
        private Dictionary<String, FrameworkElement> Fields;
        private ContextMenu LocationMenu;
        private TreeView networkList;
        private String networkType;
        private Reference.LocationType NewLocationType;
        private Guid NewLocationParent;

        public NetworkPanel() {
            this.Panel = new Grid();

            SetupLocationMenu();
            SetupSidebar();
            SetupConfigArea();
        }

        public override Panel GetPanel() {
            return Panel;
        }

        #region Context Menu Handlers
        private void Handler_AddChild(object sender, RoutedEventArgs e) {
            MenuItem parent = (MenuItem)sender;
            ContextMenu menu = (ContextMenu)parent.Parent;
            TreeViewItem item = (TreeViewItem)menu.PlacementTarget;

            TextEntryPrompt name = new TextEntryPrompt("Enter Location Details", "Enter name", "Add");
            name.confirm.Click += (a, b) => {
                name.Close();
            };

            name.ShowDialog();

            if (name.Text != "") {
                configPanel.Children.Clear();

                NewLocationType = Reference.LocationType.Public;
                this.currentItem = Guid.Empty;
                this.NewLocationParent = Guid.Parse(((TreeViewItem)item).Uid);

                IniConfigSource networkConfig = new IniConfigSource(Core.ConfigDirectory + "/Networks/" + NewLocationParent + "/network.ini");
                networkConfig.CaseSensitive = false;
                this.networkType = networkConfig.Configs["Network"].GetString("type");

                Core.Log("Adding location" + name.Text + " to network " + NewLocationParent);

                SetupEditorHeader("New Location");
                LoadConfigurationFields();
            } else {
                return;
            }
            

            
        }

        private void Handler_AddNetwork(object sender, RoutedEventArgs e) {
            currentItem = Guid.Empty;

            string[] networkTypes = Directory.GetDirectories(Core.ConfigDirectory + "/Connectors");

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

            typechooser.SelectedIndex = 0;
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

            NewLocationType = Reference.LocationType.Network;
            this.currentItem = Guid.Empty;
            LoadConfigurationFields();
        }

        private void Handler_Delete(object sender, RoutedEventArgs e) {
            MenuItem delete = (MenuItem)sender;
            ContextMenu menu = (ContextMenu)delete.Parent;
            TreeViewItem item = (TreeViewItem)menu.PlacementTarget;

            MessageBoxResult confirm = MessageBox.Show("Really delete?", "Really delete?", MessageBoxButton.YesNo);
            switch (confirm) {
                case MessageBoxResult.Yes:
                    networkList.Items.Remove(item);
                    LocationManager.DeleteLocation(Guid.Parse(item.Uid));
                    break;

                case MessageBoxResult.No:
                    MessageBox.Show("Delete aborted.");
                    return;
            }
        }

        private void Handler_LocationListClick(object sender, RoutedEventArgs e) {
            TreeViewItem selectedLocation = (TreeViewItem)sender;

            this.currentItem = Guid.Parse(selectedLocation.Uid);

            if (!Core.Networks.ContainsKey(currentItem))
                e.Handled = true;

            SetupEditor();
        }
        
        // TODO: Fix location rename redirecting to network
        private void Handler_Rename(object sender, RoutedEventArgs e) {

            e.Handled = true;

            MenuItem rename = (MenuItem)sender;
            ContextMenu menu = (ContextMenu)rename.Parent;
            TreeViewItem item = (TreeViewItem)menu.PlacementTarget;

            KeyValuePair<Guid, Location> location = LocationManager.GetLocationInfo(Guid.Parse(item.Uid));

            TextEntryPrompt prompt = new TextEntryPrompt("Rename Item", "Rename " + location.Value.Name + " to:", "Rename");
            prompt.confirm.Click += (a, b) => {
                LocationManager.RenameLocation(location.Key, prompt.Text);
                item.Header = prompt.Text;
                prompt.Close();
            };

            prompt.ShowDialog();
        }

        private void Handler_Save(object sender, RoutedEventArgs e) {

            KeyValuePair<Guid, Location> lData = LocationManager.GetLocationInfo(currentItem);

            // If id not set, assume new item and save it
            if (lData.Key == Guid.Empty) {
                SaveLocationSettings();
                return;
            }

            // Figure out if location or network selected
            TreeViewItem selected = (TreeViewItem)networkList.SelectedItem;

            switch (lData.Value.Type) {
                case Reference.LocationType.Network:
                    NetworkBot network = Core.Networks[currentItem];

                    // First, disconnect network to make sure settings are reloaded properly
                    network.Disconnect();

                    this.currentItem = network.Identifier;

                    // Then, save the network settings for that network
                    SaveLocationSettings();


                    // After that, reload the network's settings fromt he net object
                    network.ReloadConfiguration();

                    // Reconnect to network again.
                    network.Connect();
                    break;

                case Reference.LocationType.Public:

                    NetworkBot bot = Core.Networks[lData.Value.Parent];
                    bot.Network.LeaveLocation(lData.Key);

                    Core.Log("This would be where location information is saved...");

                    bot.Network.JoinLocation(lData.Key);
                    break;

                default:
                    // Not handled
                    break;
            }




        }
        #endregion

        private void LoadConfigurationFields() {
            
            String netConfigTemplateFilename = Core.ConfigDirectory + @"Connectors\" + this.networkType + "\\Config.xml";

            XmlDocument xml;
            try {
                String stackText = File.ReadAllText(netConfigTemplateFilename);
                xml = new XmlDocument();
                xml.LoadXml(stackText);

                this.Fields = new Dictionary<string, FrameworkElement>();
            }

            catch(Exception){
                Core.Log("Error loading connector structure file for network type " + networkType, LogType.ERROR);
                return;
            }

            IniConfigSource config = new IniConfigSource();

            Reference.LocationType locationType = Reference.LocationType.Unknown;

            // If we have existing data, load it in
            if (this.currentItem != Guid.Empty) {
                // Existing item
                
                Location location = LocationManager.GetLocationInfo(currentItem).Value;
                locationType = location.Type;
                switch (location.Type) {
                    case Reference.LocationType.Network:
                        config = new IniConfigSource(Core.ConfigDirectory + "/Networks/" + currentItem + "/network.ini");
                        break;

                    case Reference.LocationType.Public:
                        config = new IniConfigSource(Core.ConfigDirectory + "/Networks/" + location.Parent + "/Locations/" + currentItem + "/location.ini");
                        break;
                }
            }

            if (locationType == Reference.LocationType.Unknown) locationType = NewLocationType;

            try {
                // Load in all the nodes to generate the config editor
                foreach (XmlNode node in xml.ChildNodes[1].SelectSingleNode(locationType == Reference.LocationType.Network ? "Network" : "Location").ChildNodes) {
                    ParseConfigStructure(configPanel, config, node);
                }
            }

            catch (Exception netEx) {
                Console.WriteLine(netEx);
            }
        }

        /// <summary>
        /// Parses a single node in a configuration structure file. This is for network and location files.
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
                    if (config != null) section = config.Configs[sectionName];

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
                                tb.Text = section.GetString(n.Attributes["Key"].Value, "");
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
                                cb.IsChecked = section.GetBoolean(n.Attributes["Key"].Value, false);
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
                                p.Password = section.GetString(n.Attributes["Key"].Value, "");
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

        private void RefreshLocationList() {
            networkList.Items.Clear();
            foreach (NetworkBot b in Core.Networks.Values) {
                TreeViewItem networkListItem = new TreeViewItem();

                Location networkLocation = LocationManager.GetLocationInfo(b.Identifier).Value;

                if (networkLocation == null)
                    return;

                networkListItem.Header = networkLocation.Name;
                networkListItem.Padding = new Thickness(2);
                networkListItem.Margin = new Thickness(10, 0, 0, 2);
                networkListItem.FontWeight = FontWeights.Bold;
                networkListItem.Uid = b.Identifier.ToString();
                networkListItem.Selected += this.Handler_LocationListClick;
                networkListItem.ContextMenu = LocationMenu;

                DataTable knownChildren = LocationManager.GetChildLocations(b.Identifier);
                foreach (DataRow l in knownChildren.Rows) {
                    TreeViewItem locationItem = new TreeViewItem();
                    locationItem.Header = l["Name"].ToString();
                    locationItem.Uid = l["Identifier"].ToString();
                    locationItem.FontWeight = FontWeights.Normal;
                    locationItem.ContextMenu = LocationMenu;
                    locationItem.Selected += this.Handler_LocationListClick;

                    networkListItem.Items.Add(locationItem);
                }

                networkList.Items.Add(networkListItem);

            }
        }
        
        private void SaveLocationSettings() {
            if (currentItem == Guid.Empty) {
                this.currentItem = Guid.NewGuid();

                switch(NewLocationType){
                    case Reference.LocationType.Network:
                        // Insert into database
                        LocationManager.AddNewNetwork(currentItem, "Unnamed Network");

                        TreeViewItem newNetworkItem = new TreeViewItem();
                        newNetworkItem.Uid = currentItem.ToString();
                        newNetworkItem.Header = "Unnamed Network";
                        newNetworkItem.Selected += this.Handler_LocationListClick;
                        newNetworkItem.ContextMenu = this.LocationMenu;

                        networkList.Items.Add(newNetworkItem);
                        break;

                    case Reference.LocationType.Public:

                        LocationManager.AddNewLocation(NewLocationParent, currentItem, "Unnamed Location");

                        TreeViewItem newLocationItem = new TreeViewItem();
                        newLocationItem.Uid = currentItem.ToString();
                        newLocationItem.Header = "Unnamed Location";
                        newLocationItem.Selected += this.Handler_LocationListClick;
                        newLocationItem.ContextMenu = this.LocationMenu;
                        newLocationItem.FontWeight = FontWeights.Normal;

                        foreach (Object o in networkList.Items) {
                            TreeViewItem networkItem = (TreeViewItem)o;
                            if(networkItem.Uid == NewLocationParent.ToString())
                                networkItem.Items.Add(newLocationItem);
                        }
                        break;
                }


            } else {
                // Saving over file

                MessageBoxResult res = MessageBox.Show("Warning: By confirming this save, you're letting the existing configuration " +
                    "file be rewritten in its entirety by the data you specified here. Any custom content WILL be removed. Press okay " +
                    "to confirm." + "\n\n" +
                    "If you'd like to make notes, feel free to create new files of your own in the location folder. They won't be removed unless YOU do it.",

                    "Confirm config file overwrite?",
                    MessageBoxButton.OKCancel);

                if (res != MessageBoxResult.OK)
                    return;

                Location location = LocationManager.GetLocationInfo(currentItem).Value;

                #region Config File Setup
                IniConfigSource configuration = new IniConfigSource();

                // Depending on location type, get proper save path and recreate file
                switch (location.Type) {
                    case Reference.LocationType.Network:
                        // Saving network information
                        configuration.Save(Core.ConfigDirectory + "/Networks/" + currentItem + "/network.ini");

                        break;

                    case Reference.LocationType.Public:
                        // Saving location information
                        configuration.Save(Core.ConfigDirectory + "/Networks/" + location.Parent + "/Locations/" + currentItem + "/location.ini");
                        break;

                }

                // Add in a little warning for the people that like to hand-edit stuff
                String[] fileOverwriteComment = new string[]{
                        "# Warning: This is an automatically generated configuration file. All edits will not be saved here when the editor is used.",
                        "# To avoid loss of notes, create a different file in this directory with the notes.",
                        "",
                        ""
                    };

                File.WriteAllLines(configuration.SavePath, fileOverwriteComment);
                #endregion

                #region Save Configuration File
                // Okay. Now, let's get to saving stuff.
                // First, open that structure file back up.
                String stackText = File.ReadAllText(Core.ConfigDirectory + "/Connectors/" + this.networkType + "/Config.xml");
                XmlDocument structure = new XmlDocument();
                structure.LoadXml(stackText);

                String configType = location.Type == Reference.LocationType.Network ? "Network" : "Location";

                // If it's a network, write in the type of network into the file
                if(location.Type == Reference.LocationType.Network){
                    configuration.AddConfig("Network");
                    configuration.Configs["Network"].Set("type", this.networkType);
                }

                foreach (XmlNode section in structure.ChildNodes[1].SelectSingleNode(configType).ChildNodes) {
                    String sectionName = section.Attributes["Name"].Value;
                    configuration.AddConfig(sectionName);

                    foreach (XmlNode field in section.ChildNodes) {
                        if (field.Attributes["Type"] != null && field.Name.ToLower() == "field") {
                            String fieldKey = field.Attributes["Key"].Value;
                            switch (field.Attributes["Type"].Value.ToLower()) {
                                case "text":
                                    configuration.Configs[sectionName].Set(fieldKey, ((TextBox)Fields[fieldKey]).Text);
                                    break;

                                case "password":
                                    configuration.Configs[sectionName].Set(fieldKey, ((PasswordBox)Fields[fieldKey]).Password);
                                    break;

                                case "select":
                                    configuration.Configs[sectionName].Set(fieldKey, ((ComboBox)Fields[fieldKey]).SelectedValue.ToString());
                                    break;
                            }
                        }
                    }

                }

                configuration.Save();
                #endregion
            }

            
        }

        #region Setting up various parts
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

        private void SetupEditor() {
            Location location = LocationManager.GetLocationInfo(currentItem).Value;
            IniConfigSource config;

            switch (location.Type) {
                case Reference.LocationType.Network:

                    NetworkBot b = Core.Networks[currentItem];

                    if (!File.Exists(Core.ConfigDirectory + @"Networks\" + b.Identifier + @"\network.ini")) {
                        Core.Log("Error: Network information file not saved correctly.");

                        return;
                    }

                    config = new IniConfigSource(Core.ConfigDirectory + @"Networks\" + b.Identifier + @"\network.ini");
                    config.CaseSensitive = false;

                    this.networkType = config.Configs["Network"].GetString("type");

                    if (networkType != null && networkType != "") {


                        configPanel.Children.Clear();

                        SetupEditorHeader("Editing Network: " + location.Name);

                        TextBlock networkInfo = new TextBlock();
                        networkInfo.Margin = new Thickness(10, 8, 0, 0);

                        networkInfo.Inlines.Add(new Bold(new Run("Network Identifier: ")));
                        networkInfo.Inlines.Add(b.Identifier.ToString() + " (");
                        networkInfo.Inlines.Add(new Italic(new Run("Last modified on ")));
                        networkInfo.Inlines.Add(File.GetLastWriteTime(Core.ConfigDirectory + "/Networks/" + b.Identifier + "/network.ini") + ")");

                        configPanel.Children.Add(networkInfo);
                        LoadConfigurationFields();
                    }

                    break;

                default:

                    configPanel.Children.Clear();
                    SetupEditorHeader("Editing Location: " + location.Name);

                    if (location == null || location.Parent == Guid.Empty)
                        return;

                    TextBlock infoBlock = new TextBlock();
                    infoBlock.Margin = new Thickness(0, 8, 0, 0);

                    infoBlock.Inlines.Add(new Bold(new Run("Location Identifier: ")));
                    infoBlock.Inlines.Add(currentItem.ToString());

                    configPanel.Children.Add(infoBlock);

                    // TODO: Add location fields
                    String netConfigTemplateFilename = Core.ConfigDirectory + @"Connectors\" + networkType + @"\Config.xml";

                    try {
                        String stackText = File.ReadAllText(netConfigTemplateFilename);
                        XmlDocument xml = new XmlDocument();
                        xml.LoadXml(stackText);

                        this.Fields = new Dictionary<string, FrameworkElement>();

                        config = new IniConfigSource(Core.ConfigDirectory + @"\Networks\" + location.Parent + @"\Locations\" + currentItem + @"\location.ini");
                        config.CaseSensitive = false;

                        // Load in all the nodes to generate the config editor
                        foreach (XmlNode node in xml.ChildNodes[1].SelectSingleNode("Location").ChildNodes) {
                            ParseConfigStructure(configPanel, config, node);
                        }
                    }

                    catch (Exception netEx) {
                        Console.WriteLine(netEx);
                    }

                    break;
            }
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

        private void SetupLocationMenu() {
            this.LocationMenu = new ContextMenu();

            LocationMenu.Style = (Style)Application.Current.FindResource("ContextMenu");

            MenuItem add = new MenuItem();
            add.Header = "Add Location";
            add.Click += Handler_AddChild;
            LocationMenu.Items.Add(add);

            MenuItem rename = new MenuItem();
            rename.Header = "Rename";
            rename.Click += Handler_Rename;
            LocationMenu.Items.Add(rename);

            MenuItem delete = new MenuItem();
            delete.Header = "Delete";
            delete.Click += Handler_Delete;
            LocationMenu.Items.Add(delete);

            MenuItem duplicate = new MenuItem();
            duplicate.IsEnabled = false;
            duplicate.Header = "Duplicate";
            LocationMenu.Items.Add(duplicate);

            foreach (Object o in LocationMenu.Items) {
                MenuItem item = (MenuItem)o;
                if (item.IsEnabled)
                    item.Foreground = new SolidColorBrush(Colors.White);
                else
                    item.Foreground = new SolidColorBrush(Colors.LightPink);
            }
        }
        
        private void SetupSidebar() {
            ColumnDefinition sidebar = new ColumnDefinition();
            sidebar.Width = new GridLength(220);
            Panel.ColumnDefinitions.Add(sidebar);

            Panel.ColumnDefinitions.Add(new ColumnDefinition());


            #region Toolbar
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

            Button saveBtn = new Button();
            saveBtn.Content = "\u2713 Save settings";
            saveBtn.Style = (Style)Application.Current.FindResource("PanelActionButton");

            saveBtn.Click += Handler_Save;
            tools.Children.Add(saveBtn);

            sidebarContainer.Children.Add(tools);
            #endregion

            #region Network and Location List
            ScrollViewer networksScroller = new ScrollViewer();
            networksScroller.SetValue(Grid.RowProperty, 1);
            networksScroller.Margin = new Thickness(0, 0, 10, 0);
            networksScroller.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            sidebarContainer.Children.Add(networksScroller);


            this.networkList = new TreeView();
            networkList.BorderThickness = new Thickness(0);
            networksScroller.Content = networkList;
            networkList.Margin = new Thickness(0, 10, 0, 0);

            RefreshLocationList();
            #endregion
            
            #region Context Menu
            ContextMenu sideBarMenu = new ContextMenu();
            sideBarMenu.Style = Application.Current.FindResource("ContextMenu") as Style;

            MenuItem addNewNetwork = new MenuItem();
            addNewNetwork.Header = "Add New Network";
            addNewNetwork.Click += this.Handler_AddNetwork;
            addNewNetwork.Foreground = new SolidColorBrush(Colors.White);
            sideBarMenu.Items.Add(addNewNetwork);

            MenuItem refreshList = new MenuItem();
            refreshList.Header = "Refresh Locations";
            refreshList.Foreground = new SolidColorBrush(Colors.Tan);
            refreshList.Click += (sender, evt) => {
                RefreshLocationList();
            };
            sideBarMenu.Items.Add(refreshList);

            networkList.ContextMenu = sideBarMenu;
            #endregion
        }
        #endregion
    }
}
