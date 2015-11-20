using Ostenvighx.Suibhne.Networks.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.Events {
    public sealed class EventManager {

        /// <summary>
        /// A dictionary of extension guids that support events.
        /// The keys are the connector-supported events.
        /// The values are lists (possibly empty) that contain the guids of extensions 
        /// that support those events.
        /// </summary>
        public Dictionary<String, List<Guid>> EventSupport { get; private set; }

        public static EventManager Instance {
            get {
                if (instance == null)
                    instance = new EventManager();

                return instance;
            }

            private set { }
        }

        private static EventManager instance;

        public static void Initialize() {
            if (instance == null)
                instance = new EventManager();
        }

        private EventManager() {
            this.EventSupport = new Dictionary<string, List<Guid>>();

            GatherConnectorEvents();
        }

        private void GatherConnectorEvents() {
            if (Core.ConfigDirectory == "")
                throw new Exception("Please start by loading the configuration first, in Core.");

            String[] connectors = Directory.GetDirectories(Core.ConfigDirectory + "/Connectors/");
            foreach (String connector in connectors) {
                try {
                    String connectorName = new DirectoryInfo(connector).Name;
                    Assembly a = Assembly.LoadFile(connector + "/" + connectorName + ".dll");

                    Type[] types = a.GetTypes();
                    foreach (Type t in types) {
                        if (t.IsSubclassOf(typeof(Network))) {
                            Network network = (Network)Activator.CreateInstance(t);
                            foreach (String eventCode in network.GetSupportedEvents()) {
                                Core.Log("Attempting addition of support for event " + eventCode.ToLower() + " (Connector: " + t.Assembly.GetName().Name + ")");
                                if(!EventSupport.ContainsKey(eventCode.ToLower()))
                                    EventSupport.Add(eventCode.ToLower(), new List<Guid>());
                            }
                        }
                    }
                }

                catch (Exception) { }
            }

            if (EventSupport.ContainsKey("message_recieved"))
                EventSupport.Add("command_recieved", new List<Guid>());
        }

        /// <summary>
        /// Checks if the event list can support a set of events.
        /// </summary>
        /// <param name="eventList">The set of required events.</param>
        /// <returns>True if the system can support all the events, false otherwise.</returns>
        internal static bool VerifyCanSupport(string[] eventList) {
            if (instance == null)
                Initialize();

            if (instance.EventSupport.Count == 0)
                return false;

            IEnumerable<string> intersect = instance.EventSupport.Keys.Intersect(eventList);

            if (intersect.Count() == eventList.Count())
                return true;

            return false;
        }
        /// <summary>
        /// Should be called by the extension system after it finishes initializing and gathering all the
        /// supported events.
        /// </summary>
        /// <param name="id">A guid that specifies which extension to modify.</param>
        /// <param name="supported">An array of that extension's supported events.</param>
        public static void UpdateExtensionSupport(Guid id, string[] supported) {
            foreach (string eventCode in supported) {
                // Event not supported by system
                if (!instance.EventSupport.ContainsKey(eventCode.ToLower()))
                    continue;

                // Event already linked
                if (instance.EventSupport[eventCode.ToLower()].Contains(id))
                    continue;

                instance.EventSupport[eventCode.ToLower()].Add(id);
            }

            // Finally, remove all the references to the extension from the list that don't match the new set
            string[] unsupported = instance.EventSupport.Keys.Except(supported).ToArray<String>();
            foreach (string un in unsupported)
                instance.EventSupport[un].Remove(id);
        }
    }
}
