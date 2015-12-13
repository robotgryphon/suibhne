using Newtonsoft.Json.Linq;
using Ostenvighx.Suibhne.Extensions;
using Ostenvighx.Suibhne.Services.Chat;
using System;
using System.Collections.Generic;

namespace Ostenvighx.Suibhne.Events.Handlers {
    internal class MessageSend : EventHandler {

        public void HandleEvent(JObject json) {
            if (json["location"] == null)
                throw new Exception("Location identifier not set.");

            if (json["message"] == null || json["message"]["contents"] == null)
                throw new Exception("Need to have a message (with contents and type) defined to send!");


            Guid locationID = json["location"].ToObject<Guid>();

            Location location = LocationManager.GetLocationInfo(locationID);
            if (location == null)
                return;

            Message msg = new Message(locationID, new User(""), json["message"]["contents"].ToString());

            ChatService conn;

            if (json["message"]["is_private"] != null && (bool) json["message"]["is_private"]) {
                conn = Core.ConnectedServices[locationID] as ChatService;
                msg.IsPrivate = true;
                msg.target = new User(json["message"]["target"].ToString());
            } else {
                conn = Core.ConnectedServices[location.Parent] as ChatService;
            }

            conn.SendMessage(msg);
        }
    }
}
