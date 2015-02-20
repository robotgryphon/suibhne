using System;
using System.Collections.Generic;

using System.IO;

namespace Raindrop.Suibhne.Extensions {

    /// <summary>
    /// An extension class here is used to create useful functions and helpers for extension creation.

    /// </summary>
    public abstract class Extension {

        public static enum ResponseCodes : byte {
            /// <summary>
            /// Request for extension name, runtype, and permissions.
            /// </summary>
            Activation = 1,

            /// <summary>
            /// Request for connection details.
            /// </summary>
            ConnectionDetails = 10,

            /// <summary>
            /// Connection initialized and starting up.
            /// Includes a connection ID for tracking.
            /// </summary>
            ConnectionStart = 11,
            
            /// <summary>
            /// Connection finished connecting.
            /// Includes a connection ID for tracking.
            /// </summary>
            ConnectionComplete = 12,

            /// <summary>
            /// Connection starting disconnect.
            /// Includes a connection ID for tracking.
            /// </summary>
            ConnectionEnding = 13,

            /// <summary>
            /// Connection finished disconnecting.
            /// Includes a connection ID for tracking.
            /// </summary>
            ConnectionStopped = 14
        };

        /// <summary>
        /// The friendly name for the extension to refer to itself as.
        /// </summary>
        public String Name { get; protected set; }

        /// <summary>
        /// This is a custom identifier for the module to use.
        /// </summary>
        public Guid Identifier { get; protected set; }


        public Extension() {
            this.Name = "Plugin";
            this.Identifier = Guid.NewGuid();
        }
    }
}

