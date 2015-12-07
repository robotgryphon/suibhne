using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.Services {
    public class Events {

        /// <summary>
        /// A Network event is one that is typically fired off when the network changes in some way.
        /// </summary>
        public delegate void ServiceEvent(ServiceConnector n);

        /// <summary>
        /// Used when firing off custom events.
        /// </summary>
        /// <param name="g">The guid of the network.</param>
        /// <param name="json">Event JSON object.</param>
        public delegate void CustomEventDelegate(Guid g, String json);

    }
}
