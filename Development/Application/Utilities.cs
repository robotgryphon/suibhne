using Nini.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne {
    public class Utilities {

        public static Guid GetOrAssignIdentifier(IniConfigSource source) {
            Guid returned = Guid.Empty;

            if (source.Configs["System"] != null) {
                try {
                    returned = new Guid(source.Configs["System"].GetString("Identifier", new Guid().ToString()));
                }

                catch (Exception) {
                    Core.Log("The identifier format in file '" + source.SavePath + "' is invalid. Please don't modify the System variables unless you know what you're doing. Re-creating it..");
                    returned = Guid.NewGuid();
                    source.Configs["System"].Set("Identifier", returned);
                    source.Save();
                }
            } else {
                source.Configs.Add("System");
                returned = Guid.NewGuid();
                source.Configs["System"].Set("Identifier", returned);
                source.Save();
            }

            return returned;
        }
    }
}
