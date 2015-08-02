using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Ostenvighx.Suibhne.Gui.Panels {
    internal class OutputPanel : PanelBase {

        Grid layout;

        public OutputPanel() {
            layout = new Grid();
        }
        public override System.Windows.Controls.Panel GetPanel() {
            Grid container = new Grid();

            return container;

        }
    }
}
