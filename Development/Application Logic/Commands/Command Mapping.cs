using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ostenvighx.Suibhne.Commands {
    
    public struct CommandMap {

        /// <summary>
        /// The handler for the command.
        /// </summary>
        public String Handler;

        /// <summary>
        /// The extension the command is linked to.
        /// </summary>
        public Guid Extension;

        /// <summary>
        /// Used when the system itself is registering the handlers for the command.
        /// </summary>
        internal Boolean Is_Internal;

        /// <summary>
        /// The lowest access level the user needs to invoke the command.
        /// </summary>
        public byte AccessLevel;

        public override string ToString() {
            return this.Handler;
        }
    }
}
