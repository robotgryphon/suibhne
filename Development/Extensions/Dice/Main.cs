using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.DiceExtension {

    static class DiceLauncher {

        public static void Main(String[] args) {

            Boolean running = true;
            Socket _serv = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _serv.Connect("127.0.0.1", 6700);

            while (connected) {

            }
        }
    }
}
