using Ostenvighx.Suibhne.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.Scripting {
    public class Scripting {

        public static void GatherScriptNodes() {
            ScriptAttribute sa;
            foreach (Type type in Assembly.GetAssembly(typeof(ExtensionSystem)).GetTypes()) {
                if (type.GetCustomAttribute(typeof(ScriptAttribute)) != null) {
                    sa = (ScriptAttribute)type.GetCustomAttribute(typeof(ScriptAttribute));
                    Core.Log(">>> Found a node for '" + sa.key + "' on type '" + type.FullName + "'.");

                    Core.VariableNodes.Add(sa.key, type);
                    String rootTypeKey = sa.key;

                    foreach (MethodInfo mi in type.GetMethods()) {
                        if (mi.GetCustomAttribute(typeof(ScriptAttribute)) != null) {
                            sa = (ScriptAttribute)mi.GetCustomAttribute(typeof(ScriptAttribute));
                            Core.Log(">>> Found a node for '" + sa.key + "' on type '" + type.FullName + ":" + mi.Name + "'.");

                            Core.VariableNodes.Add(rootTypeKey + "." + sa.key, mi);
                        }
                    }

                    foreach (FieldInfo pi in type.GetFields()) {
                        if (pi.GetCustomAttribute(typeof(ScriptAttribute)) != null) {
                            sa = (ScriptAttribute)pi.GetCustomAttribute(typeof(ScriptAttribute));
                            Core.Log(">>> Found a node for '" + sa.key + "' on type '" + type.FullName + ":" + pi.Name + "'.");

                            Core.VariableNodes.Add(rootTypeKey + "." + sa.key, pi);
                        }
                    }
                }
            }
        }

    }
}
