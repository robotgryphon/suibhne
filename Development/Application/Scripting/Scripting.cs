using Ostenvighx.Suibhne.Extensions;
using Ostenvighx.Suibhne.Networks.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.Scripting {
    public class Scripting {

        public static Dictionary<String, MemberInfo> VariableNodes = new Dictionary<string, MemberInfo>();

        public static void GatherScriptNodes() {
            ScriptAttribute sa;
            foreach (Type type in Assembly.GetAssembly(typeof(ExtensionSystem)).GetTypes()) {
                if (type.GetCustomAttribute(typeof(ScriptAttribute)) != null) {
                    sa = (ScriptAttribute)type.GetCustomAttribute(typeof(ScriptAttribute));
                    Core.Log(">>> Found a node for '" + sa.key + "' on type '" + type.FullName + "'.");

                    Scripting.VariableNodes.Add(sa.key, type);
                    String rootTypeKey = sa.key;

                    foreach (MethodInfo mi in type.GetMethods()) {
                        if (mi.GetCustomAttribute(typeof(ScriptAttribute)) != null) {
                            sa = (ScriptAttribute)mi.GetCustomAttribute(typeof(ScriptAttribute));
                            Core.Log(">>> Found a node for '" + sa.key + "' on type '" + type.FullName + ":" + mi.Name + "'.");

                            Scripting.VariableNodes.Add(rootTypeKey + "." + sa.key, mi);
                        }
                    }

                    foreach (FieldInfo pi in type.GetFields()) {
                        if (pi.GetCustomAttribute(typeof(ScriptAttribute)) != null) {
                            sa = (ScriptAttribute)pi.GetCustomAttribute(typeof(ScriptAttribute));
                            Core.Log(">>> Found a node for '" + sa.key + "' on type '" + type.FullName + ":" + pi.Name + "'.");

                            Scripting.VariableNodes.Add(rootTypeKey + "." + sa.key, pi);
                        }
                    }
                }
            }
        }

        public static Message LookupVariable(String node) {
            Message response = new Message(Guid.Empty, new User(), "");
            Dictionary<String, MemberInfo> coreNodes = Scripting.VariableNodes;

            if (Scripting.VariableNodes.ContainsKey(node)) {
                MemberInfo nodeObject = Scripting.VariableNodes[node];
                response.message = "Got object: " + nodeObject.Name;

                switch (nodeObject.MemberType) {

                    case MemberTypes.Method:

                        break;

                    case MemberTypes.Field:
                        response.message = "Got field: ";
                        if (nodeObject.DeclaringType == typeof(ExtensionSystem)) {
                            Core.Log("Field lookup initiated: " + nodeObject.Name);
                            response.message += nodeObject.DeclaringType.GetField(nodeObject.Name).GetValue(ExtensionSystem.Instance).ToString();
                        }

                        if (nodeObject.DeclaringType == typeof(Core)) {
                            Core.Log("Field lookup initiated: " + nodeObject.Name);
                            response.message += nodeObject.DeclaringType.GetField(nodeObject.Name).GetValue(null).ToString();
                        }
                        break;

                    case MemberTypes.TypeInfo:

                        break;
                }

                return response;
            }
            
            return null;
        }

    }
}
