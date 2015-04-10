using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Extension_Installer {
    class Program {
        static void Main(string[] args) {

            String filename = Environment.CurrentDirectory + @"\extension";
            Console.WriteLine(filename);

            String name = "Dice Roller";
            Dictionary<String, Guid> methods = new Dictionary<string, Guid>();
            methods.Add("rollDice", Guid.NewGuid());
            Console.WriteLine(methods["rollDice"]);

            FileStream file = File.OpenWrite(filename);

            BinaryWriter bw = new BinaryWriter(file);
            bw.Write(name); bw.Flush();
            bw.Write((short)methods.Count);
            foreach (KeyValuePair<String, Guid> method in methods) {
                bw.Write(method.Key);
                bw.Write(method.Value.ToByteArray());
                bw.Flush();
            }

            bw.Close();
            file.Close();

            
            Console.ReadLine();
        }
    }
}
