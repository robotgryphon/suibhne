using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Ostenvighx.Suibhne.Services.Chat;
using Ostenvighx.Suibhne.Services;

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
                    if (json["identifier"] == null)
                        throw new FormatException("An identifier is needed to look up extension information.");


                    break;

                case "service":
                    if (json["serviceID"] == null)
                        throw new FormatException("Need to specify a service identifier.");

                    Guid loc;
                    try { loc = Guid.Parse(json["serviceID"].ToString()); }
                    catch (Exception) { throw new FormatException("Service identifier is not a valid guid.");  }

                    ServiceItem service = ServiceManager.GetServiceInfo(loc);

                    JObject serviceData = new JObject();
                    serviceData.Add("name", service.Name);
                    serviceData.Add("type", service.ServiceType);

                    // TODO: ServiceHelper.AddAdditionalServiceData();
                    // Call the service DLL to see if it has any more information to add?

                    returnData.Add("service", serviceData);
                    break;

                case "system":
                    if (json["node"] == null)
                        throw new FormatException("Need a NODE defined to get information for.");


                    break;
            }

            EventManager.HandleInternalEvent(returnData);
        }

        private async Task<JObject> GetExtensionInfo() {
            JObject returned = new JObject();

            return returned;
        }
    }
}
