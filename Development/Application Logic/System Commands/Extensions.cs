﻿using Ostenvighx.Suibhne.Extensions;
using Ostenvighx.Suibhne.Networks.Base;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.System_Commands {
    internal partial class SysCommands {

        public static void Extensions(NetworkBot conn, Message msg) {
            Message response = Message.GenerateResponse(conn.Me, msg);
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
                        response.message = "You need to specify an extension to disable.";
                        conn.SendMessage(response);
                        break;
                    }

                    workingExtension.Name = msg.message.Split(new char[] { ' ' }, 4)[3];

                    // DB query to fetch extension ID
                    try {
                        ExtensionSystem.Database.Open();
                        SQLiteCommand extensionIdFetchCommand = ExtensionSystem.Database.CreateCommand();
                        extensionIdFetchCommand.CommandText = "SELECT * FROM Extensions WHERE Name = '" + workingExtension.Name + "';";

                        SQLiteDataReader r = extensionIdFetchCommand.ExecuteReader();
                        DataTable results = new DataTable();
                        results.Load(r);

                        response.message = "I have an id of '" + results.Rows[0]["Identifier"].ToString() + "' for extension '" + workingExtension.Name + "'.";
                        conn.SendMessage(response);

                    }

                    catch (Exception e) {
                        response.message = "There was an error processing your request. Sorry about that! Error message: " + e.Message;
                        conn.SendMessage(response);

                    }

                    finally {
                        ExtensionSystem.Database.Close();
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

                    // DB query to fetch extension ID
                    try {
                        ExtensionSystem.Database.Open();
                        SQLiteCommand extensionIdFetchCommand = ExtensionSystem.Database.CreateCommand();
                        extensionIdFetchCommand.CommandText = "SELECT * FROM Extensions WHERE Name = '" + workingExtension.Name + "';";

                        SQLiteDataReader r = extensionIdFetchCommand.ExecuteReader();
                        DataTable results = new DataTable();
                        results.Load(r);

                        workingExtension.Identifier = Guid.Parse(results.Rows[0]["Identifier"].ToString());

                        if (ExtensionSystem.Instance.Extensions.ContainsKey(workingExtension.Identifier)) {
                            ExtensionSystem.Instance.ShutdownExtension(workingExtension.Identifier);
                            response.message = "Disabled extension: " + workingExtension.Name;
                            conn.SendMessage(response);
                        } else {
                            response.message = "That extension does not exist in the list. Please check the identifier and try again.";
                        }
                    }

                    catch (Exception) {

                    }

                    finally {
                        ExtensionSystem.Database.Close();
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