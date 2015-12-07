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

            if (json["message"] == null || json["message"]["contents"] == null || json["message"]["type"] == null)
                throw new Exception("Need to have a message (with contents and type) defined to send!");


            Guid locationID = json["location"].ToObject<Guid>();

            Location location = LocationManager.GetLocationInfo(locationID);
            if (location == null)
                return;

            Message msg = new Message(locationID, new User(""), json["message"]["contents"].ToString());
            ServiceWrapper bot;
            byte messageType = (byte) json["message"]["type"];

            if (json["message"]["is_private"] != null && (bool) json["message"]["is_private"]) {
                bot = Core.ConnectedServices[locationID];
                msg.IsPrivate = true;
                msg.target = new User(json["message"]["target"].ToString());
            } else {
                bot = Core.ConnectedServices[location.Parent];
            }

            bot.SendMessage(msg);
        }
    }
}
