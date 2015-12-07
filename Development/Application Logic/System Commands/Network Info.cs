using Ostenvighx.Suibhne.Services.Chat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.System_Commands {
    internal partial class SysCommands {

        public static void NetworkInfo(ServiceWrapper conn, Message msg) {
            Message response = Message.GenerateResponse(conn.Me, msg);
            String[] messageParts = msg.message.Split(' ');

            if (messageParts.Length != 3) {
                response.message = "Invalid Parameters. Format: !sys netinfo [connectionType]";
                conn.SendMessage(response);
                return;
            }

            try {
                string connType = messageParts[2];
                response.message = "Connection type recieved: " + connType;

                if (File.Exists(Core.ConfigDirectory + "Connectors/" + connType + "/" + connType + ".dll")) {
                    Assembly networkTypeAssembly = Assembly.LoadFrom(Core.ConfigDirectory + "Connectors/" + connType + "/" + connType + ".dll");
                    response.message = "Assembly information: " +
                        ((AssemblyTitleAttribute)networkTypeAssembly.GetCustomAttribute(typeof(AssemblyTitleAttribute))).Title +
                        " written by " +
                        ((AssemblyCompanyAttribute)networkTypeAssembly.GetCustomAttribute(typeof(AssemblyCompanyAttribute))).Company +
                        " (v" + networkTypeAssembly.GetName().Version + ")";

                    conn.SendMessage(response);
                } else {
                    response.message = "I couldn't find that network connector's file. Check spelling and capitaliation.";
                    conn.SendMessage(response);
                }

            }

            catch (Exception e) {
                response.message = "There was an error processing the command. (" + e.GetType().Name + ")";
                conn.SendMessage(response);
            }
        }
    }

}

