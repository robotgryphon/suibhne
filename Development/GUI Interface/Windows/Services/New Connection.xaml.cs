using Ostenvighx.Suibhne.Services;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for New_Service.xaml
    /// </summary>
    public partial class NewServiceConnection : Window {
        public NewServiceConnection() {
            InitializeComponent();
        }

        public override void EndInit() {
            base.EndInit();

            String[] serviceTypes = ServiceManager.GetRegisteredServiceTypes();
            foreach (String serviceType in serviceTypes)
                this.ConnConnector.Items.Add(serviceType);
        }

        private void Cancel(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void Submit(object sender, RoutedEventArgs e) {
            #region Error Checking
            try {
                if (ConnName.Text.Trim() == "")
                    throw new Exception("You need to fill out a name for the new connection!");

                if (ConnConnector.SelectedItem == null)
                    throw new Exception("You need to choose a service connector to use!");
            }

            catch (Exception ex) {
                MessageBox.Show(ex.Message);
                return;
            }
            #endregion

            ServiceItem @new = new ServiceItem();
            @new.Identifier = Guid.NewGuid();
            @new.Name = ConnName.Text.Trim();
            @new.ServiceType = (ConnConnector.SelectedItem as String);

            ServiceManager.Add(@new);

            this.Close();
        }
    }
}
