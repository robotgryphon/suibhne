using System;
using Newtonsoft.Json.Linq;

namespace Ostenvighx.Suibhne.Events.Handlers {
    
    /// <summary>
    /// This one really shouldn't be happening, but if it does, it means that the extension
    /// has requested that the system terminates the connection to it and removes references.
    /// </summary>
    internal class ExtensionShutdown : EventHandler {
        public void HandleEvent(JObject json) {

            Guid extID = json["extid"].ToObject<Guid>();
            Extensions.ExtensionSystem.ShutdownExtension(extID);
        }
    }
}
