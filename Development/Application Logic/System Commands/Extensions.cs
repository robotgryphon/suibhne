using Ostenvighx.Suibhne.Extensions;
using Ostenvighx.Suibhne.Services.Chat;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.System_Commands {
    internal partial class SysCommands {

        public static void Extensions(ChatService conn, Message msg) {
            Message response = Message.GenerateResponse(msg);
            String[] messageParts = msg.message.Split(' ');
            ExtensionMap workingExtension = new ExtensionMap();

            switch (messageParts[2].ToLower()) {
                case "list":
                    String[] exts = ExtensionSystem.Instance.GetActiveExtensions();

                    if (exts.Length > 0) {
                        response.message = String.Join(", ", exts);
                        conn.SendMessage(response);
                    } else {
                        response.message = "No extensions loaded.";
                        conn.SendMessage(response);
                    }
                    break;

                case "id":
                    if (messageParts.Length < 4) {
                        response.message = "You need to specify an extension to get the identifier for.";
                        conn.SendMessage(response);
                        break;
                    }

                    workingExtension.Name = msg.message.Split(new char[] { ' ' }, 4)[3];

                    try {

                        workingExtension.Identifier = ExtensionSystem.FindByName(workingExtension.Name);

                        response.message = "I have an id of '" + workingExtension.Identifier + "' for extension '" + workingExtension.Name + "'.";
                        conn.SendMessage(response);

                    }

                    catch (Exception e) {
                        response.message = "There was an error processing your request. Sorry about that! Error message: " + e.Message;
                        conn.SendMessage(response);

                    }
                    break;

                case "enable":
                    break;

                case "disable":
                    if (messageParts.Length < 4) {
                        response.message = "You need to specify an extension to disable.";
                        conn.SendMessage(response);
                        break;
                    }

                    workingExtension.Name = msg.message.Split(new char[] { ' ' }, 4)[3];

                    try {
                        workingExtension.Identifier = ExtensionSystem.FindByName(workingExtension.Name);

                        ExtensionSystem.ShutdownExtension(workingExtension.Identifier);

                        response.message = "Disabled extension: " + workingExtension.Name;
                        conn.SendMessage(response);
                    }

                    catch (Exception e) {
                        response.message = "There was a problem disabling that extension. Message: " + e.Message;
                        conn.SendMessage(response);
                    }

                    break;

                case "reload":

                    break;

                default:
                    response.message = "Unknown command. Available commands: {list, enable [ext], disable [ext], reload [type]}";
                    conn.SendMessage(response);
                    break;
            }
        }

    }
}
