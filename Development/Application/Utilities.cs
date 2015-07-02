using Nini.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using Newtonsoft.Json.Linq;

namespace Ostenvighx.Suibhne {
    public class Utilities {

        public static Guid GetOrAssignIdentifier(String nodeName) {
            string encodedFile = File.ReadAllText(Core.ConfigDirectory + "/system.sns");
            string decodedFile = Encoding.UTF8.GetString(Convert.FromBase64String(encodedFile));
            JObject systemFile = JObject.Parse(decodedFile);

            if (nodeName == null || nodeName.Trim() == "")
                return Guid.Empty;

            Guid returned = Guid.Empty;
            if (systemFile["Identifiers"] == null) {
                systemFile["Identifiers"] = new JObject();
                SaveToSystemFile(systemFile);
            }

            // Actually get/check identifier for nodeName
            if (systemFile["Identifiers"][nodeName] == null) {
                returned = Guid.NewGuid();
                systemFile["Identifiers"][nodeName] = returned.ToString();
                SaveToSystemFile(systemFile);
            } else {
                try {
                    returned = Guid.Parse((String) systemFile["Identifiers"][nodeName]);
                }

                catch (Exception) {
                    returned = Guid.NewGuid();
                    systemFile["Identifiers"][nodeName] = returned.ToString();
                    SaveToSystemFile(systemFile);   
                }
            }

            return returned;
        }

        public static void SaveToSystemFile(JObject j) {
            String converted = Convert.ToBase64String(Encoding.UTF8.GetBytes(j.ToString()));
            File.WriteAllText(Core.ConfigDirectory + "/system.sns", converted);
        }
    }
}
