using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Ostenvighx.Suibhne.Networks.Base;

namespace Ostenvighx.Suibhne.Events.Handlers {

    /// <summary>
    /// This is a complicated event that requests runtime information from the system.
    /// </summary>
    internal class InformationRequest : EventHandler {

        public void HandleEvent(JObject json) {
            JObject returnData = new JObject();
            if (json["extid"] == null && json["netid"] == null)
                throw new FormatException("Need to have either extid or netid defined for an information request.");

            if (json["token"] == null || json["token"].ToString() == "")
                throw new FormatException("Need a token defined to route the response back effectively.");

            returnData.Add("event", "information_response");

            if (json["extid"] != null)
                returnData.Add("extid", json["extid"]);

            if (json["netid"] != null)
                returnData.Add("netid", json["netid"]);

            returnData.Add("token", json["token"]);

            if (json["type"] == null || json["type"].ToString().Trim() == "")
                throw new FormatException("An information type request is needed. Valid types are 'extension', 'location', or 'system'.");

            // The type determines which type of information is being requested.
            switch (json["type"].ToString().ToLower()) {
                case "extension":

                    break;

                case "location":
                    if (json["location"] == null)
                        throw new FormatException("Need to specify a location guid.");

                    Guid loc;
                    try { loc = Guid.Parse(json["location"].ToString()); }
                    catch (Exception) { throw new FormatException("Location is not a valid guid.");  }

                    Location location = LocationManager.GetLocationInfo(loc);

                    JObject locationj = new JObject();
                    locationj.Add("name", location.Name);
                    locationj.Add("type", (byte) location.Type);
                    if (location.Parent != Guid.Empty)
                        locationj.Add("parent", location.Parent);

                    returnData.Add("location", locationj);
                    break;

                case "system":
                    if (json["node"] == null)
                        throw new FormatException("Need a NODE defined to get information for.");


                    break;
            }

            EventManager.HandleInternalEvent(returnData);
        }
    }
}
