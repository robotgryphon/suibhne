using Newtonsoft.Json.Linq;
using Ostenvighx.Suibhne.Extensions;
using Ostenvighx.Suibhne.Services.Chat;
using System;
using System.Collections.Generic;

namespace Ostenvighx.Suibhne.Events.Handlers {
    internal class MessageSend : EventHandler {

        public void HandleEvent(JObject json) {
            if (json["routing"] == null || json["routing"]["serviceID"] == null)
                throw new Exception("Routing data or service identifier not set. Aborting event handling.");

            if (json["message"] == null || json["message"]["contents"] == null)
                throw new Exception("Need to have a message (with contents and type) defined to send!");


            Guid serviceID = json["routing"]["serviceID"].ToObject<Guid>();
            if (!Core.ConnectedServices.ContainsKey(serviceID))
                throw new Exception("That service was not found or is not currently connected.");

            Message msg = new Message(json["message"]["contents"].ToString());

            ChatService conn = Core.ConnectedServices[serviceID] as ChatService;
            conn.SendMessage(json["routing"].ToString(), msg);
        }
    }
}
