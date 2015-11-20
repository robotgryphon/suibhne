using System;
using Newtonsoft.Json.Linq;

namespace Ostenvighx.Suibhne.Events.Handlers {
    internal class ExtensionShutdown : EventHandler {
        public void HandleEvent(JObject json) {

            Guid extID = json["extid"].ToObject<Guid>();
            Extensions.ExtensionSystem.ShutdownExtension(extID);
        }
    }
}
