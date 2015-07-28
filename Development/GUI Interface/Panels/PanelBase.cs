using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Ostenvighx.Suibhne.Gui.Panels {
    internal abstract class PanelBase {

        public PanelBase() { }

        public abstract Panel GetPanel();
    }
}
