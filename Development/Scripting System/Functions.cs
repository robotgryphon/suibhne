using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ostenvighx.Suibhne.Extensions;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Scripting_System {
    class Functions {

        /// <summary>
        /// Looks up a node in the system and returns a NodeMap if a matching one
        /// is found.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static NodeMap NodeLookup(String node) {
            Match test = Regex.Match(node, @"^(?<fullLookup>(?<class>[\w]+)(?:(?:\.)(?<method>[\w]+))?\s?(?<params>.*)?)$");

            if (!Global.Nodes.ContainsKey(test.Groups["fullLookup"].Value)) {
                // Node not found
                return NodeMap.None;
            }

            return Global.Nodes[node];
        }

        public void MapNodes() {
            Dictionary<String, NodeMap> nodes = new Dictionary<string, NodeMap>();

            Assembly thisAssembly = this.GetType().Assembly;

            foreach (Type t in thisAssembly.GetTypes()) {
                object[] attrs = t.GetCustomAttributes(typeof(InfoNodeAttribute), false);
                foreach (Attribute a in attrs) {
                    InfoNodeAttribute classNodeAttribute = (InfoNodeAttribute)a;

                    NodeMap classNode = new NodeMap();
                    classNode.Item = t;
                    classNode.Type = NodeType.Class;

                    nodes.Add(classNodeAttribute.key, classNode);

                    foreach (MethodInfo m in t.GetMethods()) {
                        object[] classMethodAttributes = m.GetCustomAttributes(typeof(InfoNodeAttribute), false);
                        foreach (Attribute ca in classMethodAttributes) {
                            InfoNodeAttribute cna = (InfoNodeAttribute)ca;
                            NodeMap methodNode = new NodeMap();
                            methodNode.Type = NodeType.Method;
                            methodNode.Item = m;

                            nodes.Add(classNodeAttribute.key + "." + cna.key, methodNode);
                        }
                    }
                }
            }

            Global.Nodes = nodes;
        }
    }
}

