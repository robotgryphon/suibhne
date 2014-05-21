
using System;
using System.Net;
using System.IO;

using Ostenvighx.Api.Networking.Irc;
using System.Net.Sockets;

using Mono.Data.Sqlite;
using Ostenvighx.Api.Networking;

namespace Suibhne {

	public class Launch {

		public static void Main(String[] args){

			IrcBot bot = new IrcBot();

			bot.Connect();


		}


	}

	public class IrcBot {

		public IrcConfig config;
		public IrcConnection conn;
		public SqliteConnection db;
		public StreamWriter LogFile;

		public IrcBot(){
			config = new IrcConfig("127.0.0.1", 6667, "Suibhne", "Suibhne", "Delenas Freshtt (Suibhne)", "***REMOVED***");
			conn = new IrcConnection(config);

			conn.OnMessageRecieved += Log;
			conn.OnNoticeRecieved += Log;

			// conn.OnDataRecieved += (connection, data, args) => { Console.WriteLine("Data Recieved: " + data); };

			string cs = "URI=file:data/database.db";

			db = new SqliteConnection(cs);

			this.LogFile = new StreamWriter(Environment.CurrentDirectory + "/data/log.txt", true) { AutoFlush = true };

		}

		public void Connect(){
			conn.Connect();
			conn.JoinChannel("#suibhne");
		}

		public void Log(IrcConnection conn, IrcMessage message, EventArgs args){

			Console.WriteLine(message.ToString());

			String timestamp = DateTime.Now.ToString();
			LogFile.WriteLine(String.Format("[{0}] {1}", timestamp, message.ToString()));

			/*
			 * 
			db.Open();
			using (SqliteCommand cmd = new SqliteCommand(db))
			{

				cmd.CommandText = "SELECT SQLITE_VERSION()";
				string version = Convert.ToString(cmd.ExecuteScalar());

				Console.WriteLine("SQLite version : {0}", version);
			}

			db.Close();
			*/


		}
	}
}

