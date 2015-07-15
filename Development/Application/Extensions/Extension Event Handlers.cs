﻿using Newtonsoft.Json.Linq;
using Ostenvighx.Suibhne.Networks.Base;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.Extensions {
    public static class ExtensionEventHandlers {

        public static void HandleMessageRecieved(Message m) {

            DataRow location = Utilities.GetLocationEntry(m.locationID);

            Core.Log("Handling message event: " + m.ToString().Replace(location["Identifier"].ToString(), location["Name"].ToString()));

            JObject messageEvent = new JObject();
            messageEvent.Add("responseCode", "message.recieve");

            JObject l = new JObject();
            l.Add("id", m.locationID);
            l.Add("type", (byte)m.type);

            messageEvent.Add("location", l);

            JObject user = new JObject();
            user.Add("DisplayName", m.sender.DisplayName);
            user.Add("Username", m.sender.Username);

            messageEvent.Add("user", user);

            foreach (Guid em in ExtensionSystem.Instance.MessageHandlers) {
                ExtensionSystem.Instance.Extensions[em].Send(Encoding.UTF32.GetBytes(messageEvent.ToString()));
            }
        }

        public static void HandleUserJoin(Guid l, User u) {
            DataRow location = Utilities.GetLocationEntry(l);
            Core.Log("Handling user " + u.DisplayName + " joining location " + location["Name"]);

            JObject userEvent = new JObject();
            userEvent.Add("responseCode", "user.join");
            userEvent.Add("location", l);

            JObject user = new JObject();
            user.Add("DisplayName", u.DisplayName);
            user.Add("Username", u.Username);

            userEvent.Add("user", user);

            foreach (Guid em in ExtensionSystem.Instance.UserEventHandlers) {
                ExtensionSystem.Instance.Extensions[em].Send(Encoding.UTF32.GetBytes(userEvent.ToString()));
            }
        }

        public static void HandleUserLeave(Guid l, User u) {
            DataRow location = Utilities.GetLocationEntry(l);
            Core.Log("Handling user " + u.DisplayName + " leaving location " + location["Name"]);

            JObject userEvent = new JObject();
            userEvent.Add("responseCode", "user.leave");
            userEvent.Add("location", l);

            JObject user = new JObject();
            user.Add("DisplayName", u.DisplayName);
            user.Add("Username", u.Username);

            userEvent.Add("user", user);

            foreach (Guid em in ExtensionSystem.Instance.UserEventHandlers) {
                ExtensionSystem.Instance.Extensions[em].Send(Encoding.UTF32.GetBytes(userEvent.ToString()));
            }
        }

        public static void HandleUserQuit(Guid l, User u) {
            DataRow location = Utilities.GetLocationEntry(l);
            Core.Log("Handling user " + u.DisplayName + " quitting location " + location["Name"]);

            JObject userEvent = new JObject();
            userEvent.Add("responseCode", "user.quit");
            userEvent.Add("location", l);

            JObject user = new JObject();
            user.Add("DisplayName", u.DisplayName);
            user.Add("Username", u.Username);

            userEvent.Add("user", user);

            foreach (Guid em in ExtensionSystem.Instance.UserEventHandlers) {
                ExtensionSystem.Instance.Extensions[em].Send(Encoding.UTF32.GetBytes(userEvent.ToString()));
            }
        }

        public static void HandleUserNameChange(Guid l, User u) {
            DataRow location = Utilities.GetLocationEntry(l);
            Core.Log("Handling user " + u.LastDisplayName + " changing name to " + u.DisplayName + " on network " + location["Name"]);

            JObject userEvent = new JObject();
            userEvent.Add("responseCode", "user.namechange");
            userEvent.Add("location", l);

            JObject user = new JObject();
            user.Add("LastDisplayName", u.LastDisplayName);
            user.Add("DisplayName", u.DisplayName);
            user.Add("Username", u.Username);

            userEvent.Add("user", user);

            foreach (Guid em in ExtensionSystem.Instance.UserEventHandlers) {
                ExtensionSystem.Instance.Extensions[em].Send(Encoding.UTF32.GetBytes(userEvent.ToString()));
            }
        }
    }
}
