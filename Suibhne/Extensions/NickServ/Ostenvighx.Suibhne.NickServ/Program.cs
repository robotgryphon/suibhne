using System;
using System.IO;
using System.Net.Sockets;

namespace Ostenvighx.Suibhne.NickServ {
	class MainClass {
		public static void Main(string[] args) {

			switch (args.Length) {

                case 0:
                    // Running test. Send a simple response back to the bot extension registry.
                    int port = 6700;

                    Console.WriteLine("Starting connection");

                    TcpClient conn = new TcpClient();
                    conn.Connect("127.0.0.1", 6700);
                    NetworkStream s = conn.GetStream();
                    StreamWriter o = new StreamWriter(s);

                    Console.WriteLine("Returning data");
                    o.WriteLine("Test Complete");
                    conn.Close();
                    break;

				default:
					Console.WriteLine("Need arguments for extension. Run nickserv.exe {port} {conn} {extID} {evtID} [params]");
					break;
			}
		}
	}
}
