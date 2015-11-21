using Newtonsoft.Json.Linq;
using Ostenvighx.Suibhne.Extensions;
using Ostenvighx.Suibhne.Networks.Base;
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
            ExtensionMap extension = ExtensionSystem.GetExtension(json["extid"].ToObject<Guid>());

            Location location = LocationManager.GetLocationInfo(locationID);
            if (location == null)
                return;

            Message msg = new Message(locationID, new User(extension.Name), json["message"]["contents"].ToString());
            NetworkBot bot;
            byte messageType = (byte) json["message"]["type"];

            if (Message.IsPrivateMessage((Reference.MessageType) messageType)) {
                bot = Core.Networks[locationID];
                msg.target = new User(json["message"]["target"].ToString());
            } else {
                bot = Core.Networks[location.Parent];
            }

            msg.type = (Reference.MessageType)((byte) json["message"]["type"]);
            bot.SendMessage(msg);
        }
    }
}
