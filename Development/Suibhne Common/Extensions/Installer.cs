using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using Ostenvighx.Suibhne.Extensions;
using System.IO;

namespace Ostenvighx.Suibhne.Common {
    public abstract class ExtensionInstaller {

        public static void DumpInstallData(Type extension, Boolean test = false){

            // Make sure we have an extension, not something stupid
            if (!extension.IsSubclassOf(typeof(Extension)))
                return;

            Extension e = (Extension)Activator.CreateInstance(extension);

            MethodInfo[] methods = extension.GetMethods();
            Dictionary<String, MethodInfo> CommandHandlers = new Dictionary<string, MethodInfo>();

            // Look through all the methods of an extension class, trying to get CommandHandler attributes
            foreach (MethodInfo method in methods) {
                object[] attrs = method.GetCustomAttributes(typeof(CommandHandlerAttribute), true);
                foreach (Object attr in attrs) {
                    // First attribute of type should be command handler, only one allowed anyway
                    CommandHandlerAttribute cha = (CommandHandlerAttribute)attr;
                    CommandHandlers.Add(cha.Name, method);
                }
            }


            // Create install flag file so extension doesn't try to do stupid things.
            // Hope and pray that people are checking for this...
            FileStream f = File.Create(Environment.CurrentDirectory + "/installing");
            f.Close();

            if (!test)
                GenerateInstallFile(e, CommandHandlers);
            else
                TestGenerateInstallFile(e, CommandHandlers);

            // Remove install flag file
            File.Delete(Environment.CurrentDirectory + "/installing");
        }

        private static void TestGenerateInstallFile(Extension e, Dictionary<String, MethodInfo> CommandHandlers) {
           
            // Write out extension name to file
            Console.Write(e.GetExtensionName());

            // Immediately after extension name is identifier
            Console.Write(Guid.NewGuid().ToByteArray());

            // Write out total number of command handling methods
            Console.Write((short)CommandHandlers.Count);
            foreach (KeyValuePair<String, MethodInfo> method in CommandHandlers) {
                Guid methodID = Guid.NewGuid();
                Console.WriteLine(">>> Dumping command handler '{0}', mapped to '{1}' with id '{2}'",
                    method.Key,
                    method.Value.Name,
                    methodID);

                Console.Write(method.Key);
                Console.Write(methodID.ToByteArray());
            }
        }

        private static void GenerateInstallFile(Extension e, Dictionary<String, MethodInfo> CommandHandlers) {
            FileStream file = File.OpenWrite(Environment.CurrentDirectory + "/extension");
            BinaryWriter bw = new BinaryWriter(file);

            // Write out extension name to file
            bw.Write(e.GetExtensionName());
            bw.Flush();

            // Immediately after extension name is identifier
            bw.Write(Guid.NewGuid().ToByteArray());
            bw.Flush();

            // Write out total number of command handling methods
            bw.Write((short)CommandHandlers.Count);
            foreach (KeyValuePair<String, MethodInfo> method in CommandHandlers) {
                Guid methodID = Guid.NewGuid();
                Console.WriteLine(">>> Dumping command handler '{0}', mapped to '{1}' with id '{2}'",
                    method.Key,
                    method.Value.Name,
                    methodID);

                bw.Write(method.Key);
                bw.Write(methodID.ToByteArray());
                bw.Flush();
            }

            bw.Close();
            file.Close();
        }
    }
}
