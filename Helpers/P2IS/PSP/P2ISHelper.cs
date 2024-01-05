using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarhl.FileSystem;
using System.IO.Compression;
using ClassicPersonaToolkit.Helpers.Generic;
using Yarhl.IO;

namespace ClassicPersonaToolkit.Helpers.P2IS.PSP
{
    public static class P2ISHelper
    {
        public static void ExtractPersona2Bin(string filePath)
        {
            using (var binNode = NodeFactory.FromFile(filePath))
            {
                var container = binNode.TransformWith<Formats.P2IS.PSP.BinToContainer>();
                Console.Write("Write to directory: ");
                string dir = Console.ReadLine();

                if (!Directory.Exists(dir + Path.GetFileNameWithoutExtension(filePath)))
                    Directory.CreateDirectory(dir + Path.GetFileNameWithoutExtension(filePath));

                Dictionary<int, string> fileNameList = new Dictionary<int, string>();
                foreach (var f in Navigator.IterateNodes(container))
                {
                    fileNameList.Add(Convert.ToInt32(f.Name), f.Name + ".bin");
                }
                DataStreamUtils.GenerateIndex(fileNameList, "BIN", dir + Path.GetFileNameWithoutExtension(filePath));

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

        public static void ExtractPersona2GzBin(string filePath)
        {
            using (var binNode = NodeFactory.FromFile(filePath))
            {
                var container = binNode.TransformWith<Formats.P2IS.PSP.GzBinToContainer>();
                Console.Write("Write to directory: ");
                string dir = Console.ReadLine();

                if (!Directory.Exists(dir + Path.GetFileNameWithoutExtension(filePath)))
                    Directory.CreateDirectory(dir + Path.GetFileNameWithoutExtension(filePath));

                Dictionary<int, string> fileNameList = new Dictionary<int, string>();
                foreach (var f in Navigator.IterateNodes(container))
                {                    
                    fileNameList.Add(Convert.ToInt32(f.Name), DataStreamUtils.ObtainFileNameFromGZip(f.Stream));
                }
                DataStreamUtils.GenerateIndex(fileNameList, "GZBIN", dir + Path.GetFileNameWithoutExtension(filePath));

                foreach (var f in Navigator.IterateNodes(container))
                {
                    if (dir == null)
                        dir = AppDomain.CurrentDomain.BaseDirectory;
                    var fileName = DataStreamUtils.ObtainFileNameFromGZip(f.Stream);                    
                    Console.WriteLine($"Decompressing {fileName} in {dir + Path.GetFileNameWithoutExtension(filePath)}...");
                    var decStream = DataStreamUtils.ConvertDataStreamToGZipStream(f.Stream);
                    using (FileStream fileStream = File.Create(dir + Path.GetFileNameWithoutExtension(filePath) + Path.DirectorySeparatorChar + fileName))
                    {
                        decStream.CopyTo(fileStream);
                    }
                }
                Console.Write("Finished writing! (Press Enter to continue)");
                Console.ReadLine();
            }
        }
    }
}
