using Ostenvighx.Suibhne.Services;
using Ostenvighx.Suibhne.Services.Chat;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Ostenvighx.Suibhne.Gui.Windows {
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
        }

        protected void Add(object sender, RoutedEventArgs e) {
            String[] serviceConnectors = ServiceManager.GetAllServiceConnectors();
            if(serviceConnectors.Length == 0) {
                MessageBox.Show("You don't have any service connectors registered!");
                e.Handled = true;
                return;
            }

            Windows.NewServiceConnection n = new Windows.NewServiceConnection();
            n.ShowDialog();            
        }

        
        protected void Rename(object sender, RoutedEventArgs e) {
            if (ItemsList.SelectedItem == null) {
                MessageBox.Show("Please select an item to rename.");
                e.Handled = true;
                return;
            }

            RenameDialog rnd = new RenameDialog(((ServiceItem) ItemsList.SelectedItem).Identifier);
            rnd.ShowDialog();

            PopulateLocationList();
        }
        
        private void Delete(object sender, RoutedEventArgs e) {
            if(ItemsList.SelectedItem == null) {
                MessageBox.Show("Please select a service connection to delete.");
                e.Handled = true;
                return;
            }

            ServiceItem selected = (ServiceItem) ItemsList.SelectedItem;
            MessageBoxResult result = MessageBox.Show("Are you SURE you want to delete '" + selected.Name + 
                "? This process cannot be reversed.", "Confirm Deletion", MessageBoxButton.YesNo);

            if (result != MessageBoxResult.Yes)
                return;

            ServiceManager.Delete(selected.Identifier);

            PopulateLocationList();
        }
    }
}
