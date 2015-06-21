using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ostenvighx.Suibhne.Extensions {
    class ExtensionLoader {

        public static Guid GetMethodIdentifier(String extDir, String methodName) {
            if (File.Exists(extDir + @"\extension")) {

                FileStream file = File.OpenRead(extDir + @"\extension");

                BinaryReader br = new BinaryReader(file);
                br.ReadString(); br.ReadBytes(16);
                short methods = br.ReadInt16();
                for (int methodNumber = 1; methodNumber < methods + 1; methodNumber++) {
                    String mname = br.ReadString();
                    byte[] guid = br.ReadBytes(16);
                    Guid g = new Guid(guid);
                    if (mname.ToLower() == methodName.ToLower())
                        return g;
                }

                br.Close();
                br.Dispose();
                file.Dispose();

                file = null;
                br = null;

            } else {
                Core.Log("Could not load extension; Extension information files not found. (" + extDir + ")", LogType.ERROR);
            }

            return Guid.Empty;
        }

        public static ExtensionMap LoadExtension(String extDir) {
            ExtensionMap extension = new ExtensionMap();
            extension.Ready = false;
            if (File.Exists(extDir + @"\extension")) {

                FileStream file = File.OpenRead(extDir + @"\extension");
                
                BinaryReader br = new BinaryReader(file);
                extension.Name = br.ReadString();
                extension.Identifier = new Guid(br.ReadBytes(16));


                file.Close();
                file.Dispose();
                file = null;
            } else {
                Core.Log("Could not load extension; Extension information files not found. (" + extDir + ")", LogType.ERROR);
            }

            return extension;
        }

        public static ExtensionMap[] LoadExtensions(String extDir) {
            List<ExtensionMap> extensions = new List<ExtensionMap>();
            if (Directory.Exists(extDir)) {
                String[] extensionDirectories = Directory.GetDirectories(extDir);
                foreach (String extensionDir in extensionDirectories) {
                    ExtensionMap ext = LoadExtension(extensionDir);
                    if (ext.Name != "")
                        extensions.Add(ext);
                }
            }

            return extensions.ToArray();
        }
    }
}
