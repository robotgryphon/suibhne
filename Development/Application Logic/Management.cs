using Ostenvighx.Suibhne.Networks.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Ostenvighx.Suibhne {
    public static class Management {

        public static string[] GetAvailableEventsFromConnectors() {
            List<string> eventList = new List<string>();

            if (Core.ConfigDirectory == "")
                throw new Exception("Please start by loading the configuration first, in Core.");

            String[] connectors = Directory.GetDirectories(Core.ConfigDirectory + "/Connectors/");
            foreach(String connector in connectors) {
                try {
                    String connectorName = new DirectoryInfo(connector).Name;
                    Assembly a = Assembly.LoadFile(connector + "/" + connectorName + ".dll");

                    Type[] types = a.GetTypes();
                    foreach (Type t in types) {
                        if (t.IsSubclassOf(typeof(Network))) {
                            Network network = (Network) Activator.CreateInstance(t);
                            foreach(String eventCode in network.GetSupportedEvents()) {
                                eventList.Add(eventCode);
                            }
                        }
                    }
                }

                catch (Exception) { }
            }

            return eventList.ToArray();
        }
    }
}
