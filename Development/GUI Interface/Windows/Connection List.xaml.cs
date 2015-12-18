using Ostenvighx.Suibhne.Services;
using Ostenvighx.Suibhne.Services.Chat;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Ostenvighx.Suibhne.Gui.Wins {
    /// <summary>
    /// Interaction logic for Locations.xaml
    /// </summary>
    public partial class ConnectionList : Window {

        public ConnectionList() {
            InitializeComponent();
            PopulateLocationList();
        }

        private void RefreshListHandler(object sender, RoutedEventArgs e) {
            PopulateLocationList();

            e.Handled = true;
        }


        internal void PopulateLocationList() {
            List<ServiceItem> services = ServiceManager.GetServices();
            ItemsList.ItemsSource = services;
        }

        private void Edit(object sender, RoutedEventArgs e) {
            if(ItemsList.SelectedItem == null) {
                MessageBox.Show("Please select an item to edit.");
                e.Handled = true;
                return;
            }

            ServiceItem selectedItem = (ServiceItem) ItemsList.SelectedItem;
            Core.Log("Editing service " + selectedItem.Name + " with editor GUI.");

            Windows.ConnectionEditor ce = new Windows.ConnectionEditor(selectedItem);
            ce.Show();
            // LocationEditor le = new LocationEditor(locationID);
            // le.Show();
        }

        /*

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
            Location locationInfo = ServiceManager.GetServiceInfo(locationID);
            if (locationInfo == null)
                return;

            Wins.RenameDialog rnd = new RenameDialog(locationID);
            rnd.ShowDialog();

            String new_name = rnd.text.Text;
            ServiceManager.Rename(locationID, new_name);

            PopulateLocationList();
        }

        
        private void Delete(object sender, RoutedEventArgs e) {
            TreeViewItem selectedItem = (TreeViewItem)itemsList.SelectedItem;
            if (selectedItem == null || selectedItem.Uid == null || selectedItem.Uid == Guid.Empty.ToString()) {
                MessageBox.Show("Please select an item to delete.");
                return;
            }

            Guid locationID = Guid.Parse(selectedItem.Uid);
            Location locationInfo = ServiceManager.GetServiceInfo(locationID);
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

            ServiceManager.Delete(locationID);
        } */
    }
}
