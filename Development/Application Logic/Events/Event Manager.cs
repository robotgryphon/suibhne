using Newtonsoft.Json.Linq;
using Ostenvighx.Suibhne.Extensions;
using Ostenvighx.Suibhne.Services.Chat;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.Events {
    public sealed class EventManager {

        #region Handling Delegates and Events
        public delegate void EventManagerEvent(String eventJson, Core.Side side);
        public event EventManagerEvent OnEventHandled;
        #endregion

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

        public static void HookEventHandler(ChatService n) {
            n.OnCustomEventFired += instance.HandleNetworkEvent;
        }

        /// <summary>
        /// Setup method. This method gathers all the available and supported events off the
        /// various connectors being used.
        /// </summary>
        private void GatherConnectorEvents() {
            if (Core.ConfigDirectory == "")
                throw new Exception("Please start by loading the configuration first, in Core.");

            String[] connectors = Directory.GetDirectories(Core.ConfigDirectory + "/Connectors/");
            foreach (String connector in connectors) {
                try {
                    String connectorName = new DirectoryInfo(connector).Name;
                    Assembly a = Assembly.LoadFile(connector + "/" + connectorName + ".dll");

                    int total_added_for_network_type = 0;

                    Type[] types = a.GetTypes();
                    foreach (Type t in types) {
                        if (t.IsSubclassOf(typeof(ChatService))) {
                            ChatService network = (ChatService)Activator.CreateInstance(t);
                            foreach (String eventCode in network.GetSupportedEvents()) {
                                Debug.WriteLine("Attempting addition of support for event " + eventCode.ToLower() + " (Connector: " + t.Assembly.GetName().Name + ")", "Events");
                                if (!EventSupport.ContainsKey(eventCode.ToLower())) {
                                    EventSupport.Add(eventCode.ToLower(), new List<Guid>());
                                    total_added_for_network_type++;
                                }
                            }
                        }
                    }

                    Core.Log("Added " + total_added_for_network_type + " event hooks for connector type " + a.GetName().Name);


                }

                catch (Exception) { }
            }

            // If this service can handle messages, it can handle runtime commands
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

        #region Event Routing
        /// <summary>
        /// Handles events fired off by services, delegating the tasks off to various extensions.
        /// </summary>
        /// <param name="netID">The identifier of the network that fired the event.</param>
        /// <param name="json">The event JSON object.</param>
        private void HandleNetworkEvent(Guid netID, String json) {
            if (!Core.ConnectedServices.ContainsKey(netID)) {
                Core.Log("Error handling custom event. Service guid not valid or service is not currently active. (guid: " + netID + ")\n" + json, LogType.ERROR);
                return;
            }

            try {
                JObject ev = JObject.Parse(json);
                if (ev["event"] != null && ev["event"].ToString().Trim() != "") {
                    String eventName = ev["event"].ToString().ToLower().Trim();

                    if (!instance.EventSupport.ContainsKey(eventName))
                        return;

                    foreach(Guid g in instance.EventSupport[eventName]) {
                        if (!ExtensionSystem.Instance.Extensions.ContainsKey(g))
                            continue;

                        ExtensionMap em = ExtensionSystem.Instance.Extensions[g];

                        if (em.Ready)
                            em.Send(Encoding.UTF32.GetBytes(ev.ToString()));
                    }
                }

                if (instance.OnEventHandled != null)
                    instance.OnEventHandled(ev.ToString(), Core.Side.CONNECTOR);
            }

            catch (Exception) { }
        }

        internal static void HandleExtensionEvent(JObject json) {
            string[] eventNameParts = json["event"].ToString().ToLower().Split('_');
            string eventHandler = "";
            foreach (string eventPart in eventNameParts)
                eventHandler += eventPart.Substring(0, 1).ToUpper() + eventPart.Substring(1);

            Type t = Type.GetType("Ostenvighx.Suibhne.Events.Handlers." + eventHandler);
            if (t == null) {
                // The event handler couldn't be found. Abort!
                return;
            }

            object handler = Activator.CreateInstance(t);

            (handler as Handlers.EventHandler).HandleEvent(json);

            if (instance.OnEventHandled != null)
                instance.OnEventHandled(json.ToString(), Core.Side.EXTENSION);
        }

        /// <summary>
        /// Injects an event into the system and forces it to route it accordingly.
        /// </summary>
        /// <param name="json">Injected event json.</param>
        /// <param name="destination">Which side to send the event data to.</param>
        public static void HandleInternalEvent(JObject json) {
            string[] eventNameParts = json["event"].ToString().ToLower().Split('_');
            string eventHandler = "";
            foreach (string eventPart in eventNameParts)
                eventHandler += eventPart.Substring(0, 1).ToUpper() + eventPart.Substring(1);

            Type t = Type.GetType("Ostenvighx.Suibhne.Events.Handlers." + eventHandler);
            if (t == null) {
                // The event handler couldn't be found. Abort!
                Core.Log("Error handling event " + json["event"].ToString() + "; the handler could not be found, or does not exist.");
                return;
            }

            object handler = Activator.CreateInstance(t);

            (handler as Handlers.EventHandler).HandleEvent(json);

            if (instance.OnEventHandled != null)
                instance.OnEventHandled(json.ToString(), Core.Side.INTERNAL);
        }
        #endregion
    }
}
