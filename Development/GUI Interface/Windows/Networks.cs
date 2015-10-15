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

                
            }


        }

        
    }
}
