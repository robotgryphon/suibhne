using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.Services {
    public class Reference {

        /// <summary>
        /// Shows the status of an IRC connection to a server.
        /// </summary>
        public enum ConnectionStatus : byte {

            /// <summary>
            /// Network is not ready to be connected.
            /// </summary>
            NotReady,

            /// <summary>
            /// Network is not connected, but is ready.
            /// </summary>
            Disconnected,

            /// <summary>
            /// _conn is busy in the disconnecting stage.
            /// Will automatically change to status Disconnected upon finishing
            /// and terminate the information thread.
            /// </summary>
            Disconnecting,

            /// <summary>
            /// _conn is in progress. Will automatically change to status
            /// Connected upon finishing and start the information thread.
            /// </summary>
            Connecting,

            /// <summary>
            /// _conn is finished and data is transferring in the information
            /// thread. Call Disconnect() on the connection to dispose of it.
            /// </summary>
            Connected
        };
    }
}
