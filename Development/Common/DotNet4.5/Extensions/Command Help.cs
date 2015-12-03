using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ostenvighx.Suibhne.Extensions {
    public class CommandHelpAttribute : Attribute {

        public String HelpText {
            get;
            protected set;
        }

        public CommandHelpAttribute(string text) {
            this.HelpText = text;
        }
    }
}
