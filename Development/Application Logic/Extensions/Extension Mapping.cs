using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

using Ostenvighx.Suibhne.Extensions;
using Ostenvighx.Suibhne.Services.Chat;
using Newtonsoft.Json.Linq;

namespace Ostenvighx.Suibhne.Extensions {
    
    public struct ExtensionMap {

        public static ExtensionMap None {
            get {
                return new ExtensionMap() {
                    Identifier = Guid.Empty,
                    Socket = null,
                    Name = "Invalid Extension",
                    Ready = false
                };
            }

            private set { }
        }

        public Guid Identifier;
        public Socket Socket;
        public String Name;
        public Boolean Ready;

        public void Send(byte[] data) {
            if(this.Ready && this.Socket != null && Socket.Connected)
                Socket.Send(data);
        }

        public void HandleHelpCommandRecieved(ChatService conn, Commands.CommandMap method, Message msg) {
            if (Ready) {
                JObject ev = new JObject();
                ev.Add("event", "command_help");
                ev.Add("handler", method.Handler);
                ev.Add("location", msg.locationID);

                JObject sender = new JObject();
                sender.Add("display_name", msg.sender.DisplayName);
                sender.Add("unique_id", msg.sender.UniqueID);

                ev.Add("sender", sender);

                Send(Encoding.UTF32.GetBytes(ev.ToString()));
            } else {
                Message response = Message.GenerateResponse(msg);
                response.message = "That command's extension is registered, but not started. No help is available at this point.";
                conn.SendMessage(response);
            }
        }

        public void HandleCommandRecieved(ChatService conn, Commands.CommandMap method, Message msg) {
            if (Ready) {
                JObject ev = new JObject();
                ev.Add("event", "command_received");
                ev.Add("handler", method.Handler);
                if (msg.message.Split(' ').Length > 1)
                    ev.Add("arguments", msg.message.Substring(msg.message.IndexOf(' ') + 1));
                else
                    ev.Add("arguments", "");

                JObject location = new JObject();
                location.Add("id", msg.locationID);
                location.Add("is_private", msg.IsPrivate);
                ev.Add("location", location);

                JObject sender = new JObject();
                sender.Add("display_name", msg.sender.DisplayName);
                sender.Add("unique_id", msg.sender.UniqueID);

                ev.Add("sender", sender);

                Send(Encoding.UTF32.GetBytes(ev.ToString()));
            }
        }

        public override string ToString() {
            return this.Name;
        }
    }
}
