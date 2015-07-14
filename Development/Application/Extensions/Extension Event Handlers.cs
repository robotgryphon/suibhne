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
            DataRow location = Utilities.GetLocationEntry(l);
            Core.Log("Handling user " + u.DisplayName + " joining location " + location["Name"]);

            byte[] userBytes = u.ConvertToBytes();
            byte[] toSendToExtension = new byte[18 + userBytes.Length];
            toSendToExtension[0] = (byte)Extensions.Responses.UserJoin;
            toSendToExtension[17] = byte.Parse(location["LocationType"].ToString());

            Array.Copy(l.ToByteArray(), 0, toSendToExtension, 1, 16);
            Array.Copy(userBytes, 0, toSendToExtension, 18, userBytes.Length);

            foreach (Guid em in ExtensionSystem.Instance.UserEventHandlers) {
                ExtensionSystem.Instance.Extensions[em].Send(toSendToExtension);
            }
        }

        public static void HandleUserLeave(Guid l, User u) {
            DataRow location = Utilities.GetLocationEntry(l);
            Core.Log("Handling user " + u.DisplayName + " leaving location " + location["Name"]);

            byte[] userBytes = u.ConvertToBytes();
            byte[] toSendToExtension = new byte[18 + userBytes.Length];
            toSendToExtension[0] = (byte)Extensions.Responses.UserLeave;
            toSendToExtension[17] = byte.Parse(location["LocationType"].ToString());

            Array.Copy(l.ToByteArray(), 0, toSendToExtension, 1, 16);
            Array.Copy(userBytes, 0, toSendToExtension, 18, userBytes.Length);

            foreach (Guid em in ExtensionSystem.Instance.UserEventHandlers) {
                ExtensionSystem.Instance.Extensions[em].Send(toSendToExtension);
            }
        }

        public static void HandleUserQuit(Guid l, User u) {
            DataRow location = Utilities.GetLocationEntry(l);
            Core.Log("Handling user " + u.DisplayName + " quitting location " + location["Name"]);

            byte[] userBytes = u.ConvertToBytes();
            byte[] toSendToExtension = new byte[18 + userBytes.Length];
            toSendToExtension[0] = (byte)Extensions.Responses.UserQuit;
            toSendToExtension[17] = byte.Parse(location["LocationType"].ToString());

            Array.Copy(l.ToByteArray(), 0, toSendToExtension, 1, 16);
            Array.Copy(userBytes, 0, toSendToExtension, 18, userBytes.Length);

            foreach (Guid em in ExtensionSystem.Instance.UserEventHandlers) {
                ExtensionSystem.Instance.Extensions[em].Send(toSendToExtension);
            }
        }

        public static void HandleUserNameChange(Guid l, User u) {
            DataRow location = Utilities.GetLocationEntry(l);
            Core.Log("Handling user " + u.LastDisplayName + " changing name to " + u.DisplayName + " on network " + location["Name"]);
        }
    }
}
