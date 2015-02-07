using System;
using Ostenvighx.Suibhne.Extensions;

namespace Ostenvighx.Suibhne.NickServ {
	public class NickServSuite : Extensions.ExtensionSuite {

		public NickServSuite() : base() {
			this.Authors = new string[]{ "Ted Senft" };
			this.SuiteName = "NickServ Auth Suite";
			this.Extensions = new Extension[] { new NickServ() };
		}

		public override void PrepareSuite(int port) {
			Console.WriteLine("[NS Suite] Preparing setup for connection #" + port);
		}
	}

	public class NickServ : Extensions.Extension {

		public NickServ() : base(){
			this.Name = "NickServ";
			this.Identifier = Guid.NewGuid();
		}
	}
}

