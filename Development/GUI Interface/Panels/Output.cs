using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Ostenvighx.Suibhne.Gui.Panels {
    internal class OutputPanel : PanelBase {

        Grid layout;

        TreeView LocationList;

        public OutputPanel() {
            layout = new Grid();

            SetupLayout();
        }

        private void SetupLayout() {
            
            #region Set up columns
            ColumnDefinition sidebar = new ColumnDefinition();
            sidebar.Width = new System.Windows.GridLength(220);

            ColumnDefinition main = new ColumnDefinition();

            layout.ColumnDefinitions.Add(sidebar);
            layout.ColumnDefinitions.Add(main);
            #endregion


            #region Set up sidebar
            StackPanel sidebarContainer = new StackPanel();
            sidebarContainer.SetValue(Grid.ColumnProperty, 0);

            WrapPanel globalModeContainer = new WrapPanel();
            globalModeContainer.Margin = new System.Windows.Thickness(0, 0, 0, 10);

            CheckBox globalMode = new CheckBox();
            globalMode.IsChecked = true;
            globalMode.IsEnabled = false;
            globalMode.Margin = new System.Windows.Thickness(10, 10, 0, 10);
            globalModeContainer.Children.Add(globalMode);

            Label globalModeLabel = new Label();
            globalModeLabel.Content = "Global Mode?";
            globalModeLabel.Height = 30;
            globalModeContainer.Children.Add(globalModeLabel);

            sidebarContainer.Children.Add(globalModeContainer);
            
            LocationList = new TreeView();
            sidebarContainer.Children.Add(LocationList);

            layout.Children.Add(sidebarContainer);
            #endregion
        }

        public override System.Windows.Controls.Panel GetPanel() { return layout; }
    }
}
