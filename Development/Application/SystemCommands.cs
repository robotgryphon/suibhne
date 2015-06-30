using Nini.Config;
using Ostenvighx.Suibhne.Extensions;
using Ostenvighx.Suibhne.Networks.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne {
    public class SystemCommands {

        public static void HandleCommandsCommand(NetworkBot conn, Message msg) {
            Message response = new Message(msg.locationID, conn.Me, "");
            response.type = Networks.Base.Reference.MessageType.PublicAction;
            response.message = "figures you have access to these commands: ";

            CommandMap[] AvailableCommands = CommandManager.Instance.GetAvailableCommandsForUser(msg.sender);

            response.message += String.Join(", ", AvailableCommands);
            conn.SendMessage(response);
        }

        public static void HandleExtensionsCommand(NetworkBot conn, Message msg) {
            Message response = new Message(msg.locationID, conn.Me, "");
            String[] messageParts = msg.message.Split(' ');
            String subCommand = "";

            switch (messageParts.Length) {
                case 3:
                    #region Tier 3
                    subCommand = messageParts[2];
                    switch (subCommand.ToLower()) {
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

                        default:
                            response.message = "Unknown command. Available commands: {list, enable [ext], disable [ext], reload [type]}";
                            conn.SendMessage(response);
                            break;
                    }

                    #endregion
                    break;

                case 4:
                    #region Tier 4
                    subCommand = messageParts[2];
                    switch (subCommand.ToLower()) {
                        case "enable":
                            // Used for enabling an extension that was disabled during loading
                            break;

                        case "disable":
                            // Used to disable a currently active extension
                            if (ExtensionSystem.Instance.Extensions.ContainsKey(new Guid(messageParts[3]))) {
                                ExtensionMap ext = ExtensionSystem.Instance.Extensions[new Guid(messageParts[3])];
                                ExtensionSystem.Instance.ShutdownExtensionBySocket(ext.Socket);
                                response.message = "Disabled extension: " + ext.Name;
                                conn.SendMessage(response);
                            } else {
                                response.message = "That extension does not exist in the list. Please check the identifier and try again.";
                                conn.SendMessage(response);
                            }
                            break;


                        default:
                            response.message = "Unknown command. Available commands: {enable, disable, reload}";
                            conn.SendMessage(response);
                            break;

                    }
                    #endregion
                    break;

                default:
                    response.message = "Subcommand required. Available commands: {list, enable [ext], disable [ext], reload [type]}";
                    conn.SendMessage(response);
                    break;
            }
        }

        public static void HandleVersionCommand(NetworkBot conn, Message msg) {
            Message response = new Message(msg.locationID, conn.Me, "");

            response.type = Ostenvighx.Suibhne.Networks.Base.Reference.MessageType.PublicAction;
            response.message = "is currently running version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            conn.SendMessage(response);
        }

        public static void HandleNetworkInfoCommand(NetworkBot conn, Message msg) {
            Message response = new Message(msg.locationID, conn.Me, "");
            String[] messageParts = msg.message.Split(' ');

            if (messageParts.Length != 3) {
                response.message = "Invalid Parameters. Format: !sys conninfo [connectionType]";
                conn.SendMessage(response);
                return;
            }

            try {
                string connType = messageParts[2];
                response.message = "Connection type recieved: " + connType;
                IniConfigSource configFile = new IniConfigSource(Environment.CurrentDirectory + "/suibhne.ini");
                String configDir = configFile.Configs["Suibhne"].GetString("ConfigurationRoot", Environment.CurrentDirectory + "/Configuration/").Trim();

                if (File.Exists(configDir + "NetworkTypes/" + connType + ".dll")) {
                    Assembly networkTypeAssembly = Assembly.LoadFrom(configDir + "NetworkTypes/" + connType + ".dll");
                    response.message = "Assembly information: " +
                        ((AssemblyTitleAttribute)networkTypeAssembly.GetCustomAttribute(typeof(AssemblyTitleAttribute))).Title +
                        " written by " +
                        ((AssemblyCompanyAttribute)networkTypeAssembly.GetCustomAttribute(typeof(AssemblyCompanyAttribute))).Company +
                        " (v" + networkTypeAssembly.GetName().Version + ")";

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
