using Ostenvighx.Suibhne.Services.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.System_Commands {
    internal partial class SysCommands {

        public static void Version(ChatService conn, Message msg) {
            Message response = Message.GenerateResponse(msg);
            response.message = "I am currently running version " + Core.SystemVersion.ToString();
            conn.SendMessage(response);
        }

    }
}
