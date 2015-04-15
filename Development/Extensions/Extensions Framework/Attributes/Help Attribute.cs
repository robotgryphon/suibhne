using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Raindrop.Suibhne.Extensions {
    public class HelpAttribute : Attribute {

        public String HelpText {
            get;
            protected set;
        }

        public HelpAttribute(string text) {
            this.HelpText = text;
        }
    }
}
