using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Ostenvighx.Suibhne.Extensions {
    public struct Reference {

        public static Regex MessageResponseParser = new Regex(@"^(?<sender>[^\s]+)\s(?<message>.*)$");
    }
}
