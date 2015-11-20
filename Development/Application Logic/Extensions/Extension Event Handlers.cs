using Newtonsoft.Json.Linq;
using Ostenvighx.Suibhne.Networks.Base;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ostenvighx.Suibhne.Extensions {
    public static class ExtensionEventHandlers {

        public static void HandleUserJoin(Guid l, User u) {
            JObject userEvent = new JObject();
            userEvent.Add("event", "user_joined");
            userEvent.Add("location", l);

            JObject user = new JObject();
            user.Add("DisplayName", u.DisplayName);
            user.Add("Username", u.UniqueName);

            userEvent.Add("user", user);

            foreach (Guid em in ExtensionSystem.Instance.UserEventHandlers) {
                ExtensionSystem.Instance.Extensions[em].Send(Encoding.UTF32.GetBytes(userEvent.ToString()));
            }
        }

        public static void HandleUserLeave(Guid l, User u) {
            JObject userEvent = new JObject();
            userEvent.Add("event", "user_left");
            userEvent.Add("location", l);

            JObject user = new JObject();
            user.Add("DisplayName", u.DisplayName);
            user.Add("Username", u.UniqueName);

            userEvent.Add("user", user);

            foreach (Guid em in ExtensionSystem.Instance.UserEventHandlers) {
                ExtensionSystem.Instance.Extensions[em].Send(Encoding.UTF32.GetBytes(userEvent.ToString()));
            }
        }

        public static void HandleUserQuit(Guid l, User u) {
            JObject userEvent = new JObject();
            userEvent.Add("event", "user_quit");
            userEvent.Add("location", l);

            JObject user = new JObject();
            user.Add("DisplayName", u.DisplayName);
            user.Add("Username", u.UniqueName);

            userEvent.Add("user", user);

            foreach (Guid em in ExtensionSystem.Instance.UserEventHandlers) {
                ExtensionSystem.Instance.Extensions[em].Send(Encoding.UTF32.GetBytes(userEvent.ToString()));
            }
        }

        public static void HandleUserNameChange(Guid l, User u) {
            JObject userEvent = new JObject();
            userEvent.Add("event", "user_changed");
            userEvent.Add("location", l);
            userEvent.Add("change_type", "display_name");

            JObject user = new JObject();
            user.Add("LastDisplayName", u.LastDisplayName);
            user.Add("DisplayName", u.DisplayName);
            user.Add("Username", u.UniqueName);

            userEvent.Add("user", user);

            foreach (Guid em in ExtensionSystem.Instance.UserEventHandlers) {
                ExtensionSystem.Instance.Extensions[em].Send(Encoding.UTF32.GetBytes(userEvent.ToString()));
            }
        }
    }
}
