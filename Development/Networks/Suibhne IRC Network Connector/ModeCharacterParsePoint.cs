using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.Networks.Irc {
    public struct ModeCharacterParsePoint {
        public int position;
        public Boolean is_add;
        public char modeChar;
        public String nickname;

        public override string ToString() {
            return (this.is_add ? "+" : "-") + modeChar + " " + nickname;
        }
    }
}
