using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarhl.FileSystem;
using System.IO.Compression;
using ClassicPersonaToolkit.Helpers.Generic;
using Yarhl.IO;

namespace ClassicPersonaToolkit.Helpers.P1.PSP
{
    public static class P1Helper
    {
        public static void ExtractPersona1Bin(string filePath)
        {
            using (var binNode = NodeFactory.FromFile(filePath))
            {
                var container = binNode.TransformWith<Formats.P1.PSP.P1CV1ToContainer>();
                Console.Write("Write to directory: ");
                string dir = Console.ReadLine();

                if (dir != string.Empty)
                {
                    if (!Path.EndsInDirectorySeparator(dir))
                        dir += Path.DirectorySeparatorChar;                    
                }

                if (!Directory.Exists(dir + Path.GetFileNameWithoutExtension(filePath)))
                    Directory.CreateDirectory(dir + Path.GetFileNameWithoutExtension(filePath));

                Dictionary<int, string> fileNameList = new Dictionary<int, string>();
                foreach (var f in Navigator.IterateNodes(container))
                {
                    fileNameList.Add(Convert.ToInt32(f.Name), f.Name + ".bin");
                }
                DataStreamUtils.GenerateIndex(fileNameList, "P1CV1", dir + Path.GetFileNameWithoutExtension(filePath));

                foreach (var f in Navigator.IterateNodes(container))
                {
                    if (dir == null)
                        dir = AppDomain.CurrentDomain.BaseDirectory;
                    Console.WriteLine($"Writing {f.Name} in {dir + Path.GetFileNameWithoutExtension(filePath)}...");
                    f.Stream.WriteTo(dir + Path.GetFileNameWithoutExtension(filePath) + Path.DirectorySeparatorChar + f.Name + ".bin");
                }
                Console.WriteLine("Finished writing!");
            }
        }        
    }
}
