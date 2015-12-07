using Ostenvighx.Suibhne.Services.Chat;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Ostenvighx.Suibhne.Gui.Wins {
    /// <summary>
    /// Interaction logic for Locations.xaml
    /// </summary>
    public partial class Locations : Window {

        public Locations() {
            InitializeComponent();
            GenerateContextMenu();
            PopulateLocationList();
        }

        private void GenerateContextMenu() {
            ContextMenu cm = new ContextMenu();

            MenuItem RefreshOption = new MenuItem();
            RefreshOption.Header = "Refresh List";
            RefreshOption.Click += RefreshListHandler;

            cm.Items.Add(RefreshOption);

            this.ContextMenu = cm;
        }

        private void RefreshListHandler(object sender, RoutedEventArgs e) {
            PopulateLocationList();

            e.Handled = true;
        }


        internal void PopulateLocationList() {
            itemsList.Items.Clear();
            Guid[] locations = LocationManager.GetServiceIdentifiers();
            foreach (Guid g in locations) {
                TreeViewItem networkListItem = new TreeViewItem();

                Location networkLocation = LocationManager.GetLocationInfo(g);

                if (networkLocation == null)
                    return;

                networkListItem.Header = networkLocation.Name;
                networkListItem.Padding = new Thickness(2);
                networkListItem.Margin = new Thickness(10, 4, 4, 2);
                networkListItem.FontWeight = FontWeights.Bold;
                networkListItem.Uid = g.ToString();

                Dictionary<Guid, Location> knownChildren = LocationManager.GetChildLocations(g);
                foreach (KeyValuePair<Guid, Location> location in knownChildren) {
                    TreeViewItem locationItem = new TreeViewItem();
                    locationItem.Header = location.Value.Name;
                    locationItem.Uid = location.Key.ToString();
                    locationItem.FontWeight = FontWeights.Normal;
                    networkListItem.Items.Add(locationItem);
                }

                itemsList.Items.Add(networkListItem);

            }
        }

        private void Edit(object sender, RoutedEventArgs e) {
            TreeViewItem selectedItem = (TreeViewItem)itemsList.SelectedItem;
            if (selectedItem == null || selectedItem.Uid == null || selectedItem.Uid == Guid.Empty.ToString()) {
                MessageBox.Show("Please select an item to edit.");
                return;
            }

            Guid locationID = Guid.Parse(selectedItem.Uid);
            Location locationInfo = LocationManager.GetLocationInfo(locationID);
            if (locationInfo == null)
                return;

            Core.Log("Editing " + selectedItem.Uid + " (" + locationInfo.Name + ") in gui.", LogType.GENERAL);

            LocationEditor le = new LocationEditor(locationID);
            le.Show();
        }

        protected void Add(object sender, RoutedEventArgs e) {
            New_Location n = new New_Location();
            n.ShowDialog();            
        }

        protected void Rename(object sender, RoutedEventArgs e) {
            TreeViewItem selectedItem = (TreeViewItem)itemsList.SelectedItem;
            if (selectedItem == null || selectedItem.Uid == null || selectedItem.Uid == Guid.Empty.ToString()) {
                MessageBox.Show("Please select an item to rename.");
                return;
            }

            Guid locationID = Guid.Parse(selectedItem.Uid);

            // Double check it's a valid location
            Location locationInfo = LocationManager.GetLocationInfo(locationID);
            if (locationInfo == null)
                return;

            Wins.RenameDialog rnd = new RenameDialog(locationID);
            rnd.ShowDialog();

            String new_name = rnd.text.Text;
            LocationManager.RenameLocation(locationID, new_name);

            PopulateLocationList();
        }

        private void Delete(object sender, RoutedEventArgs e) {
            TreeViewItem selectedItem = (TreeViewItem)itemsList.SelectedItem;
            if (selectedItem == null || selectedItem.Uid == null || selectedItem.Uid == Guid.Empty.ToString()) {
                MessageBox.Show("Please select an item to delete.");
                return;
            }

            Guid locationID = Guid.Parse(selectedItem.Uid);
            Location locationInfo = LocationManager.GetLocationInfo(locationID);
            if (locationInfo == null)
                return;

            MessageBoxResult result = MessageBox.Show("Are you SURE you want to delete '" + locationInfo.Name + "? This process will also delete any children.", "Comfirm Deletion", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes)
                return;

            Core.Log("Deleting item " + locationInfo.Name + " with id " + locationID + ".");
            switch (locationInfo.Type) {
                case Services.Chat.Reference.LocationType.Network:
                    ((TreeView) selectedItem.Parent).Items.Remove(selectedItem);
                    break;

                case Services.Chat.Reference.LocationType.Public:
                case Services.Chat.Reference.LocationType.Private:
                    ((TreeViewItem)selectedItem.Parent).Items.Remove(selectedItem);
                    break;

            }

            itemsList.Items.Refresh();

            LocationManager.DeleteLocation(locationID);
        }
    }
}
