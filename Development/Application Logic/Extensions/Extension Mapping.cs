using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

using Ostenvighx.Suibhne.Extensions;
using Ostenvighx.Suibhne.Services.Chat;
using Newtonsoft.Json.Linq;

namespace Ostenvighx.Suibhne.Extensions {
    
    public struct ExtensionMap {

        public static ExtensionMap None {
            get {
                return new ExtensionMap() {
                    Identifier = Guid.Empty,
                    Socket = null,
                    Name = "Invalid Extension",
                    Ready = false
                };
            }

            private set { }
        }

        public Guid Identifier;
        public Socket Socket;
        public String Name;
        public Boolean Ready;

        public void Send(byte[] data) {
            if(this.Ready && this.Socket != null && Socket.Connected)
                Socket.Send(data);
        }

        public override string ToString() {
            return this.Name;
        }
    }
}
