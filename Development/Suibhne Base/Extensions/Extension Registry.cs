using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

using Nini.Config;

using Raindrop.Suibhne.Core;
using System.Diagnostics;
using System.Threading;
using System.Net.Sockets;
using Raindrop.Api.Irc;

namespace Raindrop.Suibhne.Extensions
{

    /// <summary>
    /// The extension registry connects an IrcBot and a set of extension suites together.
    /// It goes through the extensions directory defined in the bot configuration and
    /// searches through directories for the extension INI files.
    /// </summary>
    public class ExtensionRegistry
    {

        protected IrcBot bot;

        protected Dictionary<String, Guid> RegisteredCommands;

        protected ExtensionServer server;

        public ExtensionRegistry(IrcBot bot)
        {
            this.bot = bot;

            this.server = new ExtensionServer(bot);
            this.RegisteredCommands = new Dictionary<string, Guid>();

            // InitializeExtensions();
        }

        public void InitializeExtensions()
        {
            try
            {
                String[] ExtensionDirectories = Directory.GetDirectories(bot.Configuration.ConfigDirectory + "Extensions/");

                foreach (String extDir in ExtensionDirectories)
                {
                    String extDirName = extDir.Substring(extDir.LastIndexOf("/") + 1);

                    // Attempt to find config file for extension. Start by getting all ini files.
                    String[] ExtensionFiles = Directory.GetFiles(extDir, "*.ini");
                    String foundFile = "";

                    foreach (String file in ExtensionFiles)
                    {
                        if (Path.GetFileName(file).ToLower().Equals("suite.ini"))
                        {
                            // Found file
                            foundFile = file;

                            // Now, poke the extension exe for life.
                            IniConfigSource extConfig = new IniConfigSource();
                            extConfig.Load(file);

                            String extExecutable = extConfig.Configs["ExtensionSuite"].GetString("MainExecutable").Trim();
                            if(extExecutable != ""){
                                Process.Start(extDir + "/"+ extExecutable);
                            }
                        }
                    }

                    if (foundFile == "")
                    {
                        Console.WriteLine("[Extension System] Failed to load extension suite from directory '{0}'. Suite file not found.", extDirName);
                    }
                    
                }
            }

            catch (IOException ioe)
            {
                Console.WriteLine("Failed to open directory.");
                Console.WriteLine(ioe.Message);
            }
        }

        public void HandleCommand(BotServerConnection conn, IrcMessage message) {
            String command = message.message.Split(new char[] { ' ' })[0].ToLower().TrimStart(new char[] { '!' }).TrimEnd();

            IrcMessage response = new IrcMessage(message.location, conn.Connection.Me, "Response");
            response.type = Reference.MessageType.ChannelMessage;

            switch (command) {
                case "exts":

                    string[] extCmdParts = message.message.Split(new char[] { ' ' }, 3);
                    switch (extCmdParts.Length) {
                        case 1:
                            response.message = "Invalid Parameters. Format: !ext [command]";
                            conn.Connection.SendMessage(response);
                            break;

                        case 2:
                            switch (extCmdParts[1].ToLower()) {
                                case "list":
                                    List<string> names = new List<string>();
                                    foreach (KeyValuePair<Guid, ExtensionSuiteReference> suite in server.Extensions) {
                                        names.Add(suite.Value.Name);
                                    }

                                    response.message = "Enabled extensions on bot: " + String.Join(", ", names);
                                    break;

                                default:
                                    response.message = "Unknown command.";
                                    break;
                            }

                            conn.Connection.SendMessage(response);

                            break;
                    }

                    break;

                case "version":
                    response.type = Reference.MessageType.ChannelAction;
                    response.message = "is currently running version: " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                    conn.Connection.SendMessage(response);
                    break;


                case "raw":
                    if (conn.IsBotOperator(message.sender.nickname.ToLower())) {
                        string rawCommand = message.message.Split(new char[] { ' ' }, 2)[1];
                        conn.Connection.SendRaw(rawCommand);
                    } else {
                        response.message = "You are not a bot operator. No permission to execute raw commands.";
                        conn.Connection.SendMessage(response);
                    }
                    break;

                default:

                    if (RegisteredCommands.ContainsKey(command))
                        server.SendToExtension(RegisteredCommands[command], conn.Identifier, message);
                    break;
            }
        }

    }
}

