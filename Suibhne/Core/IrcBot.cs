
using System;
using System.Net;
using System.IO;
using System.Collections.Generic;

using Ostenvighx.Api.Networking;
using Ostenvighx.Api.Networking.Irc;

using Ostenvighx.Suibhne.Plugins;

using System.Net.Sockets;
using System.Threading;

using Mono.Data.Sqlite;

namespace Ostenvighx.Suibhne.Core {

	public class IrcBot {

		public IrcConfig config;
		public IrcConnection conn;
		public SqliteConnection db;
		public StreamWriter LogFile;

		public List<String> Operators;

		public IrcBot() : this(Environment.CurrentDirectory + "/Configuration/Servers/Default.xml") {

		}

		public IrcBot(String configFile){
			config = new IrcConfig();
			config.LoadFromFile(configFile);

			conn = new IrcConnection(config);
			conn.OnMessageRecieved += Log;
			conn.OnNoticeRecieved += Log;

			conn.OnDataRecieved += (connection, data, args) => { Console.WriteLine("Data Recieved: " + data); };

			this.LogFile = new StreamWriter(Environment.CurrentDirectory + "/data/log.txt", true) { AutoFlush = true };

			this.Operators = new List<string>();

			// TODO: Add Plugin Registry
		}

		public virtual void Connect(){
			conn.Connect();

			Thread.Sleep(4000);

			// Thread.Sleep(5000); 
			// conn.Disconnect("Here we go again..");
		}

		public Boolean IsBotOperator(String nickname){
			return Operators.Contains(nickname.ToLower());
		}

		public void Log(IrcConnection conn, IrcMessage message, EventArgs args){

			Console.WriteLine(message.ToString());

			String timestamp = DateTime.Now.ToString();
			LogFile.WriteLine(String.Format("[{0}] {1}", timestamp, message.ToString()));

			if(message.message.StartsWith("!")) {
				String command = message.message.Split(new char[]{' '}, 2)[0].Substring(1).Trim().ToLower();
				Console.WriteLine(command);

				switch(command){
					case "join":
						if(IsBotOperator(message.sender)) {
							String chan = message.message.Split(new char[]{' '})[1];
							Console.WriteLine(chan);
							conn.JoinChannel(chan);
						}
						break;

						case "part":
						if(IsBotOperator(message.sender)) {
							String chan = message.message.Split(new char[]{' '})[1];
							Console.WriteLine(chan);
							conn.PartChannel(chan);
						}
						break;

						case "quit":
						if(IsBotOperator(message.sender)) {
							conn.Disconnect();
						}
						break;

						default:
						// moduleSystem.CheckCommand(command, message);

						conn.SendMessage(message.location, "Unrecognized command.");
						break;
				}


			}
		}
	}
}

