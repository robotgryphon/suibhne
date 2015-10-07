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

            Guid locationID = Guid.Parse(item.Uid);
            if (locationID == Guid.Empty)
                return;

            Location location = LocationManager.GetLocationInfo(locationID);

            TextEntryPrompt prompt = new TextEntryPrompt("Rename Item", "Rename " + location.Name + " to?", "Rename");
            prompt.confirm.Click += (a, b) => {
                LocationManager.RenameLocation(locationID, prompt.Text);
                item.Header = prompt.Text;
                prompt.Close();
            };

            e.Handled = true;
            prompt.ShowDialog();
        }

        private void Handler_Save(object sender, RoutedEventArgs e) {

            Location lData = LocationManager.GetLocationInfo(currentItem);

            // If id not set, assume new item and save it
            if (lData == null) {
                SaveLocationSettings();
                return;
            }

            // Figure out if location or network selected
            TreeViewItem selected = (TreeViewItem)networkList.SelectedItem;

            switch (lData.Type) {
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

                    NetworkBot bot = Core.Networks[lData.Parent];
                    bot.Network.LeaveLocation(currentItem);

                    Core.Log("This would be where location information is saved...");
                    // TODO: Finish location saving

                    bot.Network.JoinLocation(currentItem);
                    break;

                default:
                    // Not handled
                    break;
            }




        }
        #endregion

        private void LoadConfigurationFields() {

            try { }



            catch (Exception) {
                Core.Log("Error loading connector structure file for network type " + networkType, LogType.ERROR);
                return;
            }

            
        }

        /// <summary>
        /// Parses a single node in a configuration structure file. This is for network and location files.
        /// </summary>
        /// <param name="configPanel"></param>
        /// <param name="config"></param>
        /// <param name="n"></param>
        

        private void SaveLocationSettings() {
            if (currentItem == Guid.Empty) {
                this.currentItem = Guid.NewGuid();

                switch (NewLocationType) {
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
                            if (networkItem.Uid == NewLocationParent.ToString())
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

                Location location = LocationManager.GetLocationInfo(currentItem);

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
                if (location.Type == Reference.LocationType.Network) {
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

        
    }
}
