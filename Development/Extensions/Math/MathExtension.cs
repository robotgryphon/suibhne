using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Raindrop.Suibhne.Extensions;
using NCalc;

namespace Math_Extension {
    class MathExtension : Extension {

        public MathExtension()
            : base() {
                this.Name = "Math Parser";
                this.Authors = new string[] { "Ted Senft" };
                this.PermissionList = new byte[] { (byte)Permissions.HandleCommand };

                this.Connect();
        }

        protected override void HandleIncomingMessage(Guid connID, ExtensionsReference.MessageType messageType, string sender, string location, string message) {
            if (message.ToLower().StartsWith("!math")) {
                ParseMath(connID, sender, location, message);
            }
        }

        protected void ParseMath(Guid conn, String sender, String location, String message) {
            String[] messageParts = message.Split(new char[] { ' ' }, 2);
            if (messageParts.Length > 1) {
                try {
                    String expression = messageParts[1];
                    Expression e = new Expression(expression);

                    SendMessage(conn, ExtensionsReference.MessageType.ChannelMessage, location, "Result: " + e.Evaluate().ToString());
                }

                catch (Exception e) {
                    SendMessage(conn, ExtensionsReference.MessageType.ChannelMessage, location, "Error: " + e.Message);
                }

            } else {
                SendMessage(conn, ExtensionsReference.MessageType.ChannelMessage, location, "Invalid format. Need an expression to parse. (!math <exp>)");
            }
        }
    }
}
