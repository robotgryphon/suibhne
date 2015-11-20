using Newtonsoft.Json.Linq;
using Ostenvighx.Suibhne.Extensions;
using Ostenvighx.Suibhne.Networks.Base;
using System;
using System.Collections.Generic;

namespace Ostenvighx.Suibhne.Events.Handlers {
    internal class MessageSend : EventHandler {

        public void HandleEvent(JObject json) {
            try {
                Guid locationID = json["location"]["id"].ToObject<Guid>();
                ExtensionMap extension = ExtensionSystem.GetExtension(json["extid"].ToObject<Guid>());

                Location location = LocationManager.GetLocationInfo(locationID);
                if (location == null)
                    return;

                Message msg = new Message(locationID, new User(extension.Name), json["contents"].ToString());
                NetworkBot bot;
                if (Message.IsPrivateMessage((Reference.MessageType) json["location"]["type"].ToObject<byte>())) {
                    bot = Core.Networks[locationID];
                    msg.target = new User(json["location"]["target"].ToString());
                } else {
                    bot = Core.Networks[location.Parent];
                }

                msg.type = (Reference.MessageType)((byte) json["location"]["type"]);
                bot.SendMessage(msg);
            }

            catch (KeyNotFoundException) {
                // Network invalid or changed between requests
            }
        }
    }
}
