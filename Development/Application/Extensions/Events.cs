using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace Ostenvighx.Suibhne.Extensions {
    public abstract class Events {

        public delegate void ExtensionEvent(Extension e);

        public delegate void ExtensionMapEvent(ExtensionMap e);

        public delegate void ExtensionSocketEvent(Socket s);
        public delegate void ExtensionSocketDataEvent(Socket s, byte[] data);

    }
}

