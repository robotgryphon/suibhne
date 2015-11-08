using Ostenvighx.Suibhne.Networks.Base;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Ostenvighx.Suibhne.Gui.Wins {

    /// <summary>
    /// Interaction logic for New_Location.xaml
    /// </summary>
    public partial class New_Location : Window {

        public New_Location() {
            InitializeComponent();

            UpdateDropdown();
        }

        private void Add_Click(object sender, RoutedEventArgs e) {

            if ((this.addType.SelectedItem as ComboBoxItem) == null) {
                MessageBox.Show("Please choose the type of location you want to add, or cancel.");
                this.addType.Focus();
                return;
            }

            String addTypeString = (addType.SelectedItem as ComboBoxItem).Uid == "new_network_option" ? "network" : "location";
            ComboBoxItem type = (networkType.SelectedItem as ComboBoxItem);
            if (type == null || type.Uid == "") {
                MessageBox.Show("Please choose a " + (addTypeString == "network" ? "network type" : "parent network") + ".");
                networkType.Focus();
                return;
            }

            if(name.Text.Trim() == "") {
                MessageBox.Show("Please fill in the new location's name.");
                name.Focus();
                return;
            }

            Core.Log("Adding '" + name.Text + "' as a new " + addTypeString + "." + (addTypeString == "location" ? " (Parent: " + type.Uid + ")" : ""));

            Guid newID = Guid.NewGuid();
            switch (addTypeString) {
                case "network":
                    LocationManager.AddNewNetwork(newID, type.Content.ToString(), name.Text);
                    break;

                case "location":
                    LocationManager.AddNewLocation(Guid.Parse(type.Uid), newID, name.Text);
                    break;
            }

            LocationEditor le = new LocationEditor(newID);
            le.Save(); le.Show();

            this.Close();
        }

        private void UpdateDropdown() {
            switch ((addType.SelectedItem as ComboBoxItem).Uid) {

                case "new_location_option":
                    // Load in existing networks
                    dropLabel.Content = "Parent Network:";
                    networkType.Items.Clear();

                    foreach(Guid netID in Core.Networks.Keys) {
                        Location l = LocationManager.GetLocationInfo(netID);
                        ComboBoxItem cbi = new ComboBoxItem();
                        cbi.Content = l.Name;
                        cbi.Uid = netID.ToString();

                        networkType.Items.Add(cbi);
                    }

                    if (Core.Networks.Count > 0)
                        networkType.SelectedIndex = 0;

                    name.Focus();
                    break;

                case "new_network_option":
                    dropLabel.Content = "Network Type:";
                    networkType.Items.Clear();

                    string[] networkTypes = Directory.GetDirectories(Core.ConfigDirectory + "/Connectors");
                    foreach(string type in networkTypes) {
                        ComboBoxItem cbi = new ComboBoxItem();
                        cbi.Content = new DirectoryInfo(type).Name;
                        cbi.Uid = cbi.Content.ToString();
                        networkType.Items.Add(cbi);
                    }

                    break;
            }
        }

        /// <summary>
        /// Called when the dropdown changes (type chooser)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddChanged(object sender, System.EventArgs e) {
            UpdateDropdown();
        }
    }
}