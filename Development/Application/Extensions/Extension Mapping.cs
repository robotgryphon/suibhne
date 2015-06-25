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
            Socket.Send(data);
        }

        public byte[] PrepareStandardMessage(NetworkBot conn, Guid method, Message msg) {
            String commandArgs = "";
            if(msg.message.IndexOf(" ") != -1)
                commandArgs = msg.message.Substring(msg.message.IndexOf(" ") + 1);

            Core.Log(commandArgs, LogType.GENERAL);

            byte[] commandArgsBytes = Encoding.UTF8.GetBytes(commandArgs);
            byte[] messageOriginBytes = Encoding.UTF8.GetBytes(msg.sender.DisplayName + " ");

            byte[] data = new byte[33 + commandArgsBytes.Length + messageOriginBytes.Length];
            data[0] = (byte)Responses.Message;
            Array.Copy(msg.locationID.ToByteArray(), 0, data, 1, 16);
            Array.Copy(method.ToByteArray(), 0, data, 17, 16);
            Array.Copy(messageOriginBytes, 0, data, 33, messageOriginBytes.Length);
            Array.Copy(commandArgsBytes, 0, data, 33 + messageOriginBytes.Length, commandArgsBytes.Length);

            return data;
        }

        public void HandleHelpCommandRecieved(NetworkBot conn, Guid method, Message msg) {
            if (Ready) {
                byte[] data = PrepareStandardMessage(conn, method, msg);
                data[0] = (byte)Responses.Help;
                Send(data);
            }
        }

        public void HandleCommandRecieved(NetworkBot conn, Guid method, Message msg) {
            if (Ready) {
                byte[] data = PrepareStandardMessage(conn, method, msg);
                data[0] = (byte)Responses.Command;
                Send(data);
            }
        }

        public override string ToString() {
            return this.Name;
        }
    }
}
