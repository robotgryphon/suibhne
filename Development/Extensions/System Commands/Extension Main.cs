using Ostenvighx.Suibhne.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ostenvighx.Suibhne.Common;

namespace System_Commands {
    public class SystemCommands : Extension {
        public SystemCommands()
            : base() {
            this.Authors = new String[] { "Ted Senft" };
            this.Version = "1.0.0";
            this.Name = "System Commands";
        }

        [CommandHandler("sys")]
        public void HandleCommand(Extension e, Guid origin, string sender, string location, string args) {
            String[] arguments = args.Split(new char[] { ' ' });
            String responseMessage = "";
            switch (arguments[0]) {
                case "exts":
                    #region Extensions System Handling
                    switch (arguments.Length) {
                        case 2:
                            responseMessage = "Invalid Parameters. Format: !system exts [command]";
                            e.SendMessage(origin, Reference.MessageType.ChannelMessage, location, responseMessage);
                            break;

                        case 3:
                            String subCommand = arguments[2];
                            switch (subCommand.ToLower()) {
                                //case "list":
                                //    responseMessage = "Gathering data for global extension list. May take a minute.";
                                //    e.SendMessage(origin, Reference.MessageType.ChannelMessage, location, response);

                                //    String[] exts = GetExtensions();

                                //    if (exts.Length > 0) {
                                //        responseMessage = String.Join(", ", exts);
                                //        e.SendMessage(origin, Reference.MessageType.ChannelMessage, location, response);
                                //    } else {
                                //        responseMessage = "No extensions loaded on server.";
                                //        e.SendMessage(origin, Reference.MessageType.ChannelMessage, location, response);
                                //    }
                                //    break;

                                default:
                                    responseMessage = "Unknown command.";
                                    e.SendMessage(origin, Reference.MessageType.ChannelMessage, location, response);
                                    break;
                            }

                            break;

                        case 4:
                            // TODO: Manage extension system [enable, disable, remap commands, etc]
                            break;
                    }
                    #endregion
                    break;

                case "version":
                    response.type = Ostenvighx.Api.Irc.Reference.MessageType.ChannelAction;
                    responseMessage = "is currently running version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    e.SendMessage(origin, Reference.MessageType.ChannelMessage, location, response);
                    response.type = Ostenvighx.Api.Irc.Reference.MessageType.ChannelMessage;
                    break;


                case "raw":
                    string rawCommand = message.message.Split(new char[] { ' ' }, 3)[2];
                    conn.SendRaw(rawCommand);

                    break;

                case "uptime":
                    TimeSpan diff = DateTime.Now - StartTime;
                    response.type = Ostenvighx.Api.Irc.Reference.MessageType.ChannelAction;
                    responseMessage = "has been up for " +
                        (diff.Days > 0 ? Formatter.GetColoredText(diff.Days + " days", Formatter.Colors.Pink) + ", " : "") +
                        (diff.Hours > 0 ? Formatter.GetColoredText(diff.Hours + " hours", Formatter.Colors.Orange) + ", " : "") +
                        (diff.Minutes > 0 ? Formatter.GetColoredText(diff.Minutes + " minutes", Formatter.Colors.Green) + ", " : "") +
                        (diff.Seconds > 0 ? Formatter.GetColoredText(diff.Seconds + " seconds", Formatter.Colors.Blue) : "") + ". [Up since " + StartTime.ToString() + "]";

                    e.SendMessage(origin, Reference.MessageType.ChannelMessage, location, response);
                    response.type = Ostenvighx.Api.Irc.Reference.MessageType.ChannelMessage;
                    break;

                default:
                    response.type = Api.Irc.Reference.MessageType.ChannelAction;
                    responseMessage = "does not know what you are asking for. " + Formatter.GetColoredText("[Invalid subcommand]", Formatter.Colors.Orange);
                    e.SendMessage(origin, Reference.MessageType.ChannelMessage, location, response);
                    response.type = Ostenvighx.Api.Irc.Reference.MessageType.ChannelMessage;
                    break;
            }
        }

    }
}
