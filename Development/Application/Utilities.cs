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
            if (nodeName == null || nodeName.Trim() == "")
                return Guid.Empty;

            Guid returned = Guid.Empty;
            JObject systemFile = JObject.Parse(File.ReadAllText(Core.ConfigDirectory + "/system.json"));
            if (systemFile["identifiers"] == null) {
                systemFile["identifiers"] = new JObject();
                File.WriteAllText(Core.ConfigDirectory + "/system.json", systemFile.ToString());
            }

            // Actually get/check identifier for nodeName
            if (systemFile["identifiers"][nodeName] == null) {
                returned = Guid.NewGuid();
                systemFile["identifiers"][nodeName] = returned.ToString();
                File.WriteAllText(Core.ConfigDirectory + "/system.json", systemFile.ToString());
            } else {
                try {
                    returned = Guid.Parse((String) systemFile["identifiers"][nodeName]);
                }

                catch (Exception) {
                    returned = Guid.NewGuid();
                    systemFile["identifiers"][nodeName] = returned.ToString();
                    File.WriteAllText(Core.ConfigDirectory + "/system.json", systemFile.ToString());
                }
            }

            return returned;
        }

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
