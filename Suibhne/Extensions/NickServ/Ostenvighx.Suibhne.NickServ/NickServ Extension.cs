using System;
using Ostenvighx.Suibhne.Extensions;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Ostenvighx.Suibhne.NickServ {
	public class NickServSuite : Extensions.ExtensionSuite {

		public NickServSuite() : base() {
			this.Authors = new string[]{ "Ted Senft" };
			this.SuiteName = "NickServ Auth Suite";
			this.Extensions = new Extension[] { new NickServ() };

            PrepareSuite();

            // UnloadSuite();
            
		}
	}

	public class NickServ : Extensions.Extension {

		public NickServ() : base(){
			this.Name = "NickServ";
			this.Identifier = Guid.NewGuid();
		}
	}
}

