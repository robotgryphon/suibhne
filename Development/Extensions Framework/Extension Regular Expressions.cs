using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Raindrop.Suibhne.Extensions {
    public class RegularExpressions {

        public static Regex SuiteDetails = new Regex(@"^(?<id>[\w\-]+)\s(?<permissions>[\d]+)\s:(?<name>[\w\s]+)");
    }
}
