using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Ostenvighx.Suibhne.Gui.Panels {
    internal class OutputPanel : PanelBase {

        public override System.Windows.Controls.Panel GetPanel() {
            return new Grid();
        }
    }
}
