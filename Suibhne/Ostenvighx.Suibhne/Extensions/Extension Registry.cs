using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

using Nini.Config;

using Ostenvighx.Suibhne.Core;
using System.Diagnostics;
using System.Threading;
using System.Net.Sockets;

namespace Ostenvighx.Suibhne.Extensions
{

    /// <summary>
    /// The extension registry connects an IrcBot and a set of extension suites together.
    /// It goes through the extensions directory defined in the bot configuration and
    /// searches through directories for the extension INI files.
    /// </summary>
    public class ExtensionRegistry
    {

        protected IrcBot bot;

        /// <summary>
        /// An array of all the prepared extension suites.
        /// </summary>
        protected ExtensionSuite[] PreparedSuites;

        protected ExtensionServer server;

        public ExtensionRegistry(IrcBot bot)
        {
            this.bot = bot;
            this.PreparedSuites = new ExtensionSuite[0];

            this.server = new ExtensionServer(bot);

            // Initialize();
        }

        public void Initialize()
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

    }
}

