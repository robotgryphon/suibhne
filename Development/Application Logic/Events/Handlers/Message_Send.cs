using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.Events.Handlers {
    internal class MessageSend : EventHandler {

        public void HandleEvent(JObject json) {
            Core.Log("Handling message event: " + json.ToString());

            // throw new NotImplementedException();
        }
    }
}
