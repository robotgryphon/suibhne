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
        }

        public static void SendShutdownRequest(ExtensionMap em) {
            byte[] shutdownBytes = new byte[17];
            shutdownBytes[0] = (byte)Responses.Remove;
            Array.Copy(ExtensionSystem.Instance.Identifier.ToByteArray(), 0, shutdownBytes, 1, 16);

            em.Send(shutdownBytes);
        }
    }
}
