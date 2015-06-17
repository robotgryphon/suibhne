using Ostenvighx.Suibhne.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ostenvighx.Suibhne.Common;

namespace System_Commands {
    public class SystemCommands : Extension {

        [CommandHandler("system")]
        public void HandleCommand(Extension e, Guid origin, string sender, string args) {
            String[] arguments = args.Split(new char[] { ' ' });
            String responseMessage = "";
            
            String[] messageParts = args.Split(new char[] { ' ' });
            String command = messageParts[0];
            String subCommand = "";
            if (messageParts.Length > 1)
                subCommand = messageParts[1].ToLower();

            switch(command){

                default:

                    break;
            }
        }

        private void uptimeCommand(Extension e, Guid origin, string sender, string args){
            // TimeSpan diff = DateTime.Now - StartTime;
            TimeSpan diff = TimeSpan.Zero;
            Ostenvighx.Suibhne.Networks.Base.Message response = new Ostenvighx.Suibhne.Networks.Base.Message(
                origin,
                new Ostenvighx.Suibhne.Networks.Base.User(sender),
                "");

            response.type = Ostenvighx.Suibhne.Networks.Base.Reference.MessageType.PublicAction;
            response.message = "has been up for " +
                (diff.Days > 0 ? diff.Days + " days" : "") +
                (diff.Hours > 0 ? diff.Hours + " hours, " : "") +
                (diff.Minutes > 0 ? diff.Minutes + " minutes, " : "") +
                (diff.Seconds > 0 ? diff.Seconds + " seconds" : ""); // + ". [Up since " + StartTime.ToString() + "]";

            // conn.SendMessage(response);
        }

        
                        // If our user is in the operators list
                        if (message.sender.AuthLevel >= (byte) User.AccessLevel.BotAdmin) {
                            switch (subCommand) {
                                case "exts":
                                case "extensions":
                                    #region Extensions System Handling
                                    switch (messageParts.Length) {
                                        case 3:
                                            #region Tier 3
                                            subCommand = messageParts[2];
                                            switch (subCommand.ToLower()) {
                                                case "list":
                                                    String[] exts = GetExtensions();

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
                                                    break;

                                                case "reload":
                                                    switch (messageParts[3]) {
                                                        case "commands":
                                                            if (File.Exists(this.ConfigFilename)) {
                                                                DateTime lastUpdate = File.GetLastWriteTime(ConfigFilename);
                                                                if (lastUpdate > ConfigLastUpdate) {
                                                                    int numRemapped = MapCommands();
                                                                    response.message = "Successfully remapped " + numRemapped + " conmmands to " + Extensions.Count  + " extensions.";
                                                                    conn.SendMessage(response);
                                                                } else {
                                                                    response.message = "Your extension config is up-to-date. No need to remap commands.";
                                                                    conn.SendMessage(response);
                                                                }
                                                            }
                                                            break;

                                                        case "extensions":
                                                            // WIP
                                                            break;

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
                                    #endregion
                                    break;

                                case "version":
                                    response.type = Ostenvighx.Suibhne.Networks.Base.Reference.MessageType.PublicAction;
                                    response.message = "is currently running version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                                    conn.SendMessage(response);
                                    response.type = Ostenvighx.Suibhne.Networks.Base.Reference.MessageType.PublicMessage;
                                    break;

                                case "conninfo":
                                    // Get info about connection and system
                                    if (messageParts.Length != 3) {
                                        response.message = "Invalid Parameters. Format: !sys conninfo [connectionType]";
                                        conn.SendMessage(response);
                                        break;
                                    }

                                    try {
                                        string connType = messageParts[2];
                                        response.message = "Connection type recieved: " + connType;
                                        IniConfigSource configFile = new IniConfigSource(Environment.CurrentDirectory + "/suibhne.ini");
                                        String configDir = configFile.Configs["Suibhne"].GetString("ConfigurationRoot", Environment.CurrentDirectory + "/Configuration/").Trim();

                                        if (File.Exists(configDir + "NetworkTypes/" + connType + ".dll")) {
                                            Assembly networkTypeAssembly = Assembly.LoadFrom(configDir + "NetworkTypes/" + connType + ".dll");
                                            response.message = "Assembly information: " + 
                                                ((AssemblyTitleAttribute) networkTypeAssembly.GetCustomAttribute(typeof(AssemblyTitleAttribute))).Title + 
                                                " written by " + 
                                                ((AssemblyCompanyAttribute) networkTypeAssembly.GetCustomAttribute(typeof(AssemblyCompanyAttribute))).Company + 
                                                " (v" + networkTypeAssembly.GetName().Version + ")";

                                            conn.SendMessage(response);
                                        }
                                        
                                    }

                                    catch(Exception e){
                                        response.message = "There was an error processing the command. (" + e.GetType().Name + ")";
                                        conn.SendMessage(response);
                                    }
                                    break;

                                case "uptime":
                                    
                                    break;

                                default:
                                    response.type = Suibhne.Networks.Base.Reference.MessageType.PublicAction;
                                    response.message = "does not know what you are asking for. "; // + "[Invalid subcommand]", Formatter.Colors.Orange);
                                    conn.SendMessage(response);
                                    response.type = Ostenvighx.Suibhne.Networks.Base.Reference.MessageType.PublicMessage;
                                    break;
                            }
                        } else {
                            response.message = "Error: " + "You must be a bot operator to run the system command.";
                            conn.SendMessage(response);
                        }
                    } else {
                        response.message = "Available system commands: {exts/extensions, version, conninfo, uptime}";
                        conn.SendMessage(response);
                    }

    }
}
