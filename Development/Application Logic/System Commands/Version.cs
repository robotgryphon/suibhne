using Ostenvighx.Suibhne.Networks.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.System_Commands {
    internal partial class SysCommands {

        public static void Version(NetworkBot conn, Message msg) {
            Message response = Message.GenerateResponse(conn.Me, msg);
            if (Message.IsPrivateMessage(response))
                response.type = Networks.Base.Reference.MessageType.PrivateAction;
            else
                response.type = Networks.Base.Reference.MessageType.PublicAction;

            response.message = "is currently running version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            conn.SendMessage(response);
        }

    }
}
