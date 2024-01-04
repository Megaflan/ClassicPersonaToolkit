using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarhl.FileSystem;
using Yarhl.IO;

namespace ClassicPersonaToolkit.Helpers.P2IS.PSP
{
    public static class P2ISHelper
    {
        public static void  ExtractPersona2Bin(string filePath)
        {
            using (var binNode = NodeFactory.FromFile(filePath))
            {
                var container = binNode.TransformWith<Formats.P2IS.PSP.BinToContainer>();
                Console.Write("Write to directory: ");
                string dir = Console.ReadLine();                
                foreach (var f in Navigator.IterateNodes(container))
                {
                    if (dir == null)
                        dir = AppDomain.CurrentDomain.BaseDirectory;
                    Console.WriteLine($"Writing {f.Name} in {dir + Path.GetFileNameWithoutExtension(filePath)}...");
                    f.Stream.WriteTo(dir + Path.GetFileNameWithoutExtension(filePath) + Path.DirectorySeparatorChar + f.Name + ".bin");
                }
                Console.Write("Finished writing! (Press Enter to continue)");
                Console.ReadLine();
            }
        }

    }
}
