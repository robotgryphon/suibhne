using Ostenvighx.Suibhne.Commands;
using Ostenvighx.Suibhne.Services.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.System_Commands {
    internal partial class SysCommands {

        // TODO: This requires the old action type, but this may not be supported on all services. Need to refactor..
        public static void Commands(ChatService conn, Message msg) {
            Message response = Message.GenerateResponse(msg);
            response.message = "I figure you have access to these commands: ";

            String[] AvailableCommands = CommandManager.Instance.GetAvailableCommandsForUser(msg.sender, false);

            response.message += String.Join(", ", AvailableCommands);
            conn.SendMessage(response);
        }
    }
}
