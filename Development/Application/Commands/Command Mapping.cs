using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ostenvighx.Suibhne.Commands {
    
    public struct CommandMap {

        public String CommandString;
        public Guid Extension;
        public byte AccessLevel;

        public override string ToString() {
            return this.CommandString;
        }
    }
}
