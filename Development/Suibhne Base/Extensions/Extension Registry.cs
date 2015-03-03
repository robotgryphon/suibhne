using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

using Nini.Config;

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

            InitializeExtensions();
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
                        if (Path.GetFileName(file).ToLower().Equals("extension.ini"))
                        {
                            // Found file
                            foundFile = file;

                            // Now, poke the extension exe for life.
                            IniConfigSource extConfig = new IniConfigSource();
                            extConfig.Load(file);

                            String extExecutable = extConfig.Configs["Extension"].GetString("MainExecutable").Trim();
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
            String[] messageParts = message.message.Split(new char[] { ' ' });
            String command = messageParts[0].ToLower().TrimStart(new char[] { '!' }).TrimEnd();
            String subCommand = "";
            if (messageParts.Length > 1)
                subCommand = messageParts[1].ToLower();

            IrcMessage response = new IrcMessage(message.location, conn.Connection.Me, "Response");
            response.type = Api.Irc.IrcReference.MessageType.ChannelMessage;

            switch (command) {
                case "exts":
                    switch (messageParts.Length) {
                        case 1:
                            response.message = "Invalid Parameters. Format: !exts [command]";
                            conn.Connection.SendMessage(response);
                            break;

                        case 2:
                            switch (subCommand.ToLower()) {
                                case "list":
                                    response.message = "Gathering data. May take a minute.";
                                    conn.Connection.SendMessage(response);
                                    server.ShowDetails(conn.Identifier, message.sender.nickname, message.location);
                                    break;

                                default:
                                    response.message = "Unknown command.";
                                    conn.Connection.SendMessage(response);
                                    break;
                            }

                            break;

                        case 3:
                            switch (subCommand.ToLower()) {

                                default:

                                    break;

                            }

                            break;
                    }

                    break;

                case "version":
                    response.type = Api.Irc.IrcReference.MessageType.ChannelAction;
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

        public void Shutdown() {
            server.Shutdown();
        }

    }
}

