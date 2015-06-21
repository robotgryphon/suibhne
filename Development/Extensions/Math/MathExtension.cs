using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Ostenvighx.Suibhne.Extensions;
using NetworkBase = Ostenvighx.Suibhne.Networks.Base;
using NCalc;

namespace Math_Extension {
    class MathExtension : Extension {

        [CommandHandler("parseMath"), Help("Give a basic mathematical expression to parse. (1 + 2 * 3, etc)")]
        public void ParseMath(Extension ext, Guid origin, String sender, String arguments) {
            try {
                Expression e = new Expression(arguments);

                ext.SendMessage(origin, NetworkBase.Reference.MessageType.PublicMessage, "Result: " + e.Evaluate().ToString());
            }

            catch (Exception e) {
                ext.SendMessage(origin, NetworkBase.Reference.MessageType.PublicMessage, "Error: " + e.Message);
            }
        }
    }
}
