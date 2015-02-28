using Raindrop.Api.Irc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Raindrop.Suibhne.Extensions {
    
    public class ExtensionSuiteReference {

        public Guid Identifier;
        public Socket Socket;
        public String Name;

        public void Send(byte[] data) {
            Socket.Send(data);
        }

        public void SendString(String data) {
            byte[] buff = Encoding.ASCII.GetBytes(data);
            try { Socket.Send(buff); }
            catch (Exception e) {

            }
        }

        public void SendMessage(byte connID, IrcMessage msg){

        }
    }
}
