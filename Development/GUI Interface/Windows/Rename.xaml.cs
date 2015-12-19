using Ostenvighx.Suibhne.Services;
using Ostenvighx.Suibhne.Services.Chat;
using System;
using System.Windows;

namespace Ostenvighx.Suibhne.Gui.Windows {
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

            ServiceItem l = ServiceManager.GetServiceInfo(id);

            this.text.Text = l.Name;
        }

        private void Confirm(object sender, RoutedEventArgs e) {
            if (this.text.Text.Trim() == "")
                return;

            ServiceManager.Rename(id, text.Text.Trim());

            this.Close();
        }
    }
}
