using Ostenvighx.Suibhne.Networks.Base;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Ostenvighx.Suibhne.Gui.Windows {
    /// <summary>
    /// Interaction logic for Locations.xaml
    /// </summary>
    public partial class Locations : Window {

        public Locations() {
            InitializeComponent();
            PopulateLocationList();
        }

        internal void PopulateLocationList() {
            itemsList.Items.Clear();
            foreach (NetworkBot b in Core.Networks.Values) {
                TreeViewItem networkListItem = new TreeViewItem();

                Location networkLocation = LocationManager.GetLocationInfo(b.Identifier);

                if (networkLocation == null)
                    return;

                networkListItem.Header = networkLocation.Name;
                networkListItem.Padding = new Thickness(2);
                networkListItem.Margin = new Thickness(10, 0, 0, 2);
                networkListItem.FontWeight = FontWeights.Bold;
                networkListItem.Uid = b.Identifier.ToString();
                Dictionary<Guid, Location> knownChildren = LocationManager.GetChildLocations(b.Identifier);
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

        private void Add(object sender, RoutedEventArgs e) {

        }

        private void Delete(object sender, RoutedEventArgs e) {

        }
    }
}
