using Raindrop.Api.Irc;
using Raindrop.Suibhne.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Raindrop.Suibhne.Extensions {
    
    public class ExtensionReference {

        public Guid Identifier;
        public Socket Socket;
        public String Name;

        public void Send(byte[] data) {
            Socket.Send(data);
        }

        public void SendString(String data) {
            byte[] buff = Encoding.ASCII.GetBytes(data);
            Send(buff);
        }

        public void SendMessage(byte connID, IrcMessage msg){
            try {
                String message = msg.location + " " + msg.sender + " :" + msg.message;
                byte[] messageBytes = Encoding.UTF8.GetBytes(message);

                byte[] messageRaw = new byte[3 + messageBytes.Length];

                // Set up message prefix
                messageRaw[0] = (byte)Extension.ResponseCodes.Message;
                messageRaw[1] = connID;
                messageRaw[2] = (byte)msg.type;

                Array.Copy(messageBytes, 0, messageRaw, 3, messageBytes.Length);

                Socket.Send(messageRaw);
            }
            catch (SocketException se) {
                Console.WriteLine(se);
            }
        }

        public void HandleCommandRecieved(BotServerConnection conn, IrcMessage message) {
            SendMessage(conn.Identifier, message);
        }
    }
}
