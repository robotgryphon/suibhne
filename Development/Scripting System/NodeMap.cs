using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripting_System {

    public enum NodeType : byte {
        Class,
        Method,
        Property,
        Invalid
    }

    public struct NodeMap {

        public Object Item;
        public NodeType Type;

        public static NodeMap None {
            get {
                NodeMap map = new NodeMap();
                map.Type = NodeType.Invalid;
                return map;
            }

            private set { }
        }
    }
}
