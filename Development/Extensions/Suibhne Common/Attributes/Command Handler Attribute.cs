using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ostenvighx.Suibhne.Extensions {

    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class CommandHandlerAttribute : Attribute {

        public string Name {
            get;
            protected set;
        }

        public CommandHandlerAttribute(String name) {
            this.Name = name;
        }
    }
}
