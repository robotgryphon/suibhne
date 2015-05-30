using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ostenvighx.Suibhne.Extensions {
    
    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = false, Inherited=false)]
    public class InfoNodeAttribute : Attribute {
        public String key { get; protected set; }
        public String text { get; protected set; }

        public InfoNodeAttribute(String key) : this(key, "No information is available for this node.") { }

        public InfoNodeAttribute(String key, String text) {
            this.key = key;
            this.text = text;
        }
    }

}
