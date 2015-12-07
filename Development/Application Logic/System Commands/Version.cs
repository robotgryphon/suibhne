using Ostenvighx.Suibhne.Services.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.System_Commands {
    internal partial class SysCommands {

        public static void Version(ServiceWrapper conn, Message msg) {
            Message response = Message.GenerateResponse(conn.Me, msg);
            response.message = "I am currently running version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            conn.SendMessage(response);
        }

    }
}
