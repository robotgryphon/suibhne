using Ostenvighx.Suibhne.Networks.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.Extensions {
    internal class ExtensionHelper {

        public static byte[] PrepareCommandMessage(NetworkBot conn, Guid method, Message msg) {
            String commandArgs = "";
            if (msg.message.IndexOf(" ") != -1)
                commandArgs = msg.message.Substring(msg.message.IndexOf(" ") + 1);

            msg.message = commandArgs;
            byte[] commandData = Extension.PrepareMessage(method, msg);
            commandData[0] = (byte)Responses.Command;
            return commandData;

            //byte[] commandArgsBytes = Encoding.UTF8.GetBytes(commandArgs);
            //byte[] messageOriginBytes = Encoding.UTF8.GetBytes(msg.sender.DisplayName + " ");

            //byte[] data = new byte[33 + commandArgsBytes.Length + messageOriginBytes.Length];
            //data[0] = (byte)Responses.Command;
            //Array.Copy(msg.locationID.ToByteArray(), 0, data, 1, 16);
            //Array.Copy(method.ToByteArray(), 0, data, 17, 16);
            //Array.Copy(messageOriginBytes, 0, data, 33, messageOriginBytes.Length);
            //Array.Copy(commandArgsBytes, 0, data, 33 + messageOriginBytes.Length, commandArgsBytes.Length);

            //return data;
        }

        public static void SendShutdownRequest(ExtensionMap em) {
            byte[] shutdownBytes = new byte[17];
            shutdownBytes[0] = (byte)Responses.Remove;
            Array.Copy(ExtensionSystem.Instance.Identifier.ToByteArray(), 0, shutdownBytes, 1, 16);

            em.Send(shutdownBytes);
        }
    }
}
