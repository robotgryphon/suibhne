using Ostenvighx.Suibhne.Commands;
using Ostenvighx.Suibhne.Networks.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.System_Commands {
    internal partial class SysCommands {

        public static void Commands(NetworkBot conn, Message msg) {
            Message response = Message.GenerateResponse(conn.Me, msg);
            if (Message.IsPrivateMessage(response))
                response.type = Networks.Base.Reference.MessageType.PrivateAction;
            else
                response.type = Networks.Base.Reference.MessageType.PublicAction;

            response.message = "figures you have access to these commands: ";

            String[] AvailableCommands = CommandManager.Instance.GetAvailableCommandsForUser(msg.sender, false);

            response.message += String.Join(", ", AvailableCommands);
            conn.SendMessage(response);
        }
    }
}
