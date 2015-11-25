using Ostenvighx.Suibhne.Networks.Base;
using System;
using System.Windows;

namespace Ostenvighx.Suibhne.Gui.Wins {
    /// <summary>
    /// Interaction logic for RenameDialog.xaml
    /// </summary>
    public partial class RenameDialog : Window {

        private Guid id;

        private RenameDialog() {
            InitializeComponent();
        }

        public RenameDialog(Guid id) {
            this.id = id;
            InitializeComponent();
        }

        public override void EndInit() {
            base.EndInit();

            Location l = LocationManager.GetLocationInfo(id);

            this.text.Text = l.Name;
        }
        private void Confirm(object sender, RoutedEventArgs e) {
            if (this.text.Text.Trim() == "")
                return;

            this.Close();
        }
    }
}
