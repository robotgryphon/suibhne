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
                                if(!instance.EventSupport.ContainsKey(eventCode.ToLower()))
                                    instance.EventSupport.Add(eventCode.ToLower(), new List<Guid>());
                            }
                        }
                    }
                }

                catch (Exception) { }
            }
        }

        /// <summary>
        /// Should be called by the extension system after it finishes initializing and gathering all the
        /// supported events.
        /// </summary>
        /// <param name="supported">Key is the extension guid. Value is a list of that extension's supported events.</param>
        public void UpdateExtensionSupport(Dictionary<Guid, string[]> supported) {

        }
    }
}
