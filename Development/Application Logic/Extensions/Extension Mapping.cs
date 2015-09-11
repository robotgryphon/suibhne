using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

using Ostenvighx.Suibhne.Extensions;
using Ostenvighx.Suibhne.Networks.Base;
using Newtonsoft.Json.Linq;

namespace Ostenvighx.Suibhne.Extensions {
    
    public struct ExtensionMap {

        public Guid Identifier;
        public Socket Socket;
        public String Name;
        public Boolean Ready;

        public void Send(byte[] data) {
            if(this.Ready && this.Socket != null && Socket.Connected)
                Socket.Send(data);
        }

        public void HandleHelpCommandRecieved(NetworkBot conn, Commands.CommandMap method, Message msg) {
            if (Ready) {
                JObject ev = new JObject();
                ev.Add("event", "command.help");
                ev.Add("handler", method.CommandString);
                
                JObject location = new JObject();
                location.Add("id", msg.locationID);
                location.Add("type", (byte) msg.type);

                ev.Add("location", location);

                JObject sender = new JObject();
                sender.Add("DisplayName", msg.sender.DisplayName);
                sender.Add("Username", msg.sender.Username);

                ev.Add("sender", sender);

                Send(Encoding.UTF32.GetBytes(ev.ToString()));
            } else {
                Message response = Message.GenerateResponse(conn.Me, msg);
                response.message = "That command's extension is registered, but not started. No help is available at this point.";
                conn.SendMessage(response);
            }
        }

        public void HandleCommandRecieved(NetworkBot conn, Commands.CommandMap method, Message msg) {
            if (Ready) {
                JObject ev = new JObject();
                ev.Add("event", "command.recieve");
                ev.Add("handler", method.CommandString);
                if (msg.message.Split(' ').Length > 1)
                    ev.Add("arguments", msg.message.Substring(msg.message.IndexOf(' ') + 1));
                else
                    ev.Add("arguments", "");

                JObject location = new JObject();
                location.Add("id", msg.locationID);
                location.Add("type", (byte)msg.type);

                ev.Add("location", location);

                JObject sender = new JObject();
                sender.Add("DisplayName", msg.sender.DisplayName);
                sender.Add("Username", msg.sender.Username);

                ev.Add("sender", sender);

                Send(Encoding.UTF32.GetBytes(ev.ToString()));
            }
        }

        public override string ToString() {
            return this.Name;
        }
    }
}
