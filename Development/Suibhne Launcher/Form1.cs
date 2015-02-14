using Raindrop.Suibhne.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Raindrop.Api.Irc;
using Raindrop.Suibhne.Core;

namespace Suibhne_Launcher {
    public partial class Form1 : Form {

        delegate void SetTextCallback(string text);

        private void SetText(string text) {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (output.InvokeRequired) {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            } else {
                output.Text += text + "\n";
            }
        }

        public Form1() {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e) {
            IrcBot bot = new IrcBot();
            bot.OnMessageRecieved += HandleMessage;

            bot.LoadServers();
            bot.Start();
        }

        public void HandleMessage(IrcConnection conn, IrcMessage msg) {
            SetText(msg.ToString());
        }
    }
}
