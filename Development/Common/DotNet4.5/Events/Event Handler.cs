using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.Events {
    internal interface EventHandler {

        void HandleEvent(JObject json);

    }
}
