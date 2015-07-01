using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ostenvighx.Suibhne.Extensions {
    public enum Responses : byte {
        /// <summary>
        /// Request for extension name, runtype, and permissions.
        /// </summary>
        Activation = 1,

        /// <summary>
        /// Request for extension information. (Name, author, version, etc)
        /// </summary>
        Details = 2,

        Permissions = 3,

        /// <summary>
        /// Called when bot or extension requests a reactivation
        /// of extension.
        /// </summary>
        Restart = 4,

        /// <summary>
        /// Called when a bot requests an extension be disabled 
        /// at runtime.
        /// </summary>
        Remove = 5,

        /// <summary>
        /// Used when an extension should handle a message.
        /// </summary>
        Command = 6,

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
        ConnectionStopped = 14,

        Message = 20,

        LocationGet = 30,

        Help = 21
    };
}
