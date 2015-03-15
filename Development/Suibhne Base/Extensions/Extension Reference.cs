using Raindrop.Api.Irc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

using Raindrop.Suibhne.Extensions;

namespace Raindrop.Suibhne {
    
    public struct ExtensionReference {

        public Guid Identifier;
        public Socket Socket;
        public String Name;

        public void Send(byte[] data) {
            Socket.Send(data);
        }

        public void HandleCommandRecieved(IrcBot conn, Message msg) {
            Console.WriteLine(Name + " handling command: " + msg.message);
            byte[] message = Extension.PrepareMessage(conn.Identifier, Identifier, (byte) msg.type, msg.location, msg.sender.nickname, msg.message);
            Send(message);
        }
    }
}
