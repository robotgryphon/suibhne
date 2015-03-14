using Raindrop.Suibhne.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Raindrop.Suibhne.Tester {

    public static class Launcher {

        
        [STAThread]
        static void Main() {

            Guid origin = Guid.NewGuid();
            Guid destination = Guid.NewGuid();

            Console.WriteLine("Original: " + origin + " ===> " + destination);

            byte[] data = Extension.PrepareMessage(origin, destination, 1, "#channel", "Sender", "Message");

            string location, sender, message;
            Guid finalDestination, finalOrigin;
            byte finalType;

            Extension.ParseMessage(data, out finalOrigin, out finalDestination, out finalType, out location, out sender, out message);

            Console.WriteLine("Original: " + finalOrigin + " ===> " + finalDestination);

            Console.ReadLine();

        }
    }
}
