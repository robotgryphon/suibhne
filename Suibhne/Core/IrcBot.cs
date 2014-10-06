
using System;
using System.Net;
using System.IO;
using System.Collections.Generic;

using Ostenvighx.Api.Networking;
using Ostenvighx.Api.Networking.Irc;

using Ostenvighx.Suibhne.Plugins;

using System.Net.Sockets;
using System.Threading;

namespace Ostenvighx.Suibhne.Core {

	public class IrcBot {

		public IrcConfig config;
		public IrcConnection conn;

		public StreamWriter LogFile;

		public List<String> Operators;

		public delegate void IrcCommandEvent(IrcBot bot, IrcMessage message);

		public event IrcCommandEvent OnCommandRecieved;

		public IrcBot() : this(Environment.CurrentDirectory + "/Configuration/Default/Server.json") {

		}

		public IrcBot(IrcConfig config) {
			Setup(config);
		}

		public IrcBot(String configFile) {
			config = new IrcConfig();
			config.LoadFromFile(configFile);

			Setup(config);

			// TODO: Add Plugin Registry
		}

		protected void Setup(IrcConfig config) {
			conn = new IrcConnection(config);
			conn.OnMessageRecieved += Log;
			conn.OnNoticeRecieved += Log;

			conn.OnDataRecieved += (connection, data, args) => {
				Console.WriteLine("Data Recieved: " + data);
			};

			this.LogFile = new StreamWriter(Environment.CurrentDirectory + "/data/log.txt", true) { AutoFlush = true };

			this.Operators = new List<string>();
		}

		public virtual void Connect() {
			conn.Connect();
		}

		public Boolean IsBotOperator(String nickname) {
			return Operators.Contains(nickname.ToLower());
		}

		protected void HandleCommand(IrcMessage message) {
			if(this.OnCommandRecieved != null) {
				OnCommandRecieved(this, message);
			}

			String command = message.message.Split(new char[]{ ' ' }, 2)[0].Substring(1).Trim().ToLower();
			Console.WriteLine("Command recieved from " + message.sender + ": " + command);
		}

		//TODO: Rename or move these commands
		public void Log(IrcConnection conn, IrcMessage message, EventArgs args) {

			Console.WriteLine(message.ToString());

			String timestamp = DateTime.Now.ToString();
			LogFile.WriteLine(String.Format("[{0}] {1}", timestamp, message.ToString()));

			if(message.message.StartsWith("!"))
				HandleCommand(message);
		}
	}
}

