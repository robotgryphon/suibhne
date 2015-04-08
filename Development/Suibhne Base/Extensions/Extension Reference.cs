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
        public List<Guid> Methods;
        public Boolean Ready;

        public void Send(byte[] data) {
            Socket.Send(data);
        }

        public void HandleCommandRecieved(IrcBot conn, Guid method, Message msg) {
            if (Ready) {
                String commandArgs = msg.message.Substring(msg.message.IndexOf(" ") + 1);
                Console.WriteLine(commandArgs);

                byte[] commandArgsBytes = Encoding.UTF8.GetBytes(commandArgs);
                byte[] messageOriginBytes = Encoding.UTF8.GetBytes(msg.location + " " + msg.sender.nickname + " ");

                byte[] data = new byte[33 + commandArgsBytes.Length + messageOriginBytes.Length];
                data[0] = (byte)Extension.ResponseCodes.Command;
                Array.Copy(conn.Identifier.ToByteArray(), 0, data, 1, 16);
                Array.Copy(method.ToByteArray(), 0, data, 17, 16);
                Array.Copy(messageOriginBytes, 0, data, 33, messageOriginBytes.Length);
                Array.Copy(commandArgsBytes, 0, data, 33 + messageOriginBytes.Length, commandArgsBytes.Length);

                Send(data);

            }
        }
    }
}
