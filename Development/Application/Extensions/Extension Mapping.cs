using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

using Ostenvighx.Suibhne.Extensions;
using Ostenvighx.Suibhne.Networks.Base;

namespace Ostenvighx.Suibhne.Extensions {
    
    public struct ExtensionMap {

        public Guid Identifier;
        public Socket Socket;
        public String Name;
        public Boolean Ready;

        public void Send(byte[] data) {
            if(Socket.Connected)
                Socket.Send(data);
        }

        public void HandleHelpCommandRecieved(NetworkBot conn, Guid method, Message msg) {
            if (Ready) {
                byte[] data = ExtensionHelper.PrepareCommandMessage(conn, method, msg);
                data[0] = (byte)Responses.Help;
                Send(data);
            }
        }

        public void HandleCommandRecieved(NetworkBot conn, Guid method, Message msg) {
            if (Ready) {
                byte[] data = ExtensionHelper.PrepareCommandMessage(conn, method, msg);
                data[0] = (byte)Responses.Command;
                Send(data);
            }
        }

        public override string ToString() {
            return this.Name;
        }
    }
}
