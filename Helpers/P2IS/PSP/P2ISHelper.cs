using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using Yarhl.FileSystem;
using ClassicPersonaToolkit.Helpers.Generic;
using Yarhl.IO;
using Yarhl.Media.Text;

namespace ClassicPersonaToolkit.Helpers.P2IS.PSP
{
    public static class P2ISHelper
    {
        static string NormalizeOutputDir(string outputDir)
        {
            string dir = (outputDir ?? string.Empty).Trim().Trim('"');

            if (string.IsNullOrWhiteSpace(dir))
                dir = AppDomain.CurrentDomain.BaseDirectory;

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!Path.EndsInDirectorySeparator(dir))
                dir += Path.DirectorySeparatorChar;

            return dir;
        }

        public static void ExtractPersona2Bin(string filePath, string outputDir = null)
        {
            using (var binNode = NodeFactory.FromFile(filePath))
            {
                var container = binNode.TransformWith<Formats.P2IS.PSP.BinToContainer>();

                string baseDir = NormalizeOutputDir(outputDir);
                string targetDir = Path.Combine(baseDir, Path.GetFileNameWithoutExtension(filePath));
                Directory.CreateDirectory(targetDir);

                Dictionary<int, string> fileNameList = new Dictionary<int, string>();
                foreach (var f in Navigator.IterateNodes(container))
                    fileNameList.Add(Convert.ToInt32(f.Name), f.Name + ".bin");

                DataStreamUtils.GenerateIndex(fileNameList, "BIN", targetDir);

                foreach (var f in Navigator.IterateNodes(container))
                {
                    string outPath = Path.Combine(targetDir, f.Name + ".bin");
                    Console.WriteLine($"Writing {f.Name} in {targetDir}...");
                    f.Stream.WriteTo(outPath);
                }

                Console.WriteLine("Finished writing!");
            }
        }

        public static void ExtractPersona2GzBin(string filePath, string outputDir = null)
        {
            using (var binNode = NodeFactory.FromFile(filePath))
            {
                var container = binNode.TransformWith<Formats.P2IS.PSP.GzBinToContainer>();

                string baseDir = NormalizeOutputDir(outputDir);
                string targetDir = Path.Combine(baseDir, Path.GetFileNameWithoutExtension(filePath));
                Directory.CreateDirectory(targetDir);

                Dictionary<int, string> fileNameList = new Dictionary<int, string>();
                foreach (var f in Navigator.IterateNodes(container))
                    fileNameList.Add(Convert.ToInt32(f.Name), DataStreamUtils.ObtainFileNameFromGZip(f.Stream));

                DataStreamUtils.GenerateIndex(fileNameList, "GZBIN", targetDir);

                foreach (var f in Navigator.IterateNodes(container))
                {
                    var fileName = DataStreamUtils.ObtainFileNameFromGZip(f.Stream);
                    Console.WriteLine($"Decompressing {fileName} in {targetDir}...");

                    var decStream = DataStreamUtils.ConvertDataStreamToGZipStream(f.Stream);
                    string outPath = Path.Combine(targetDir, fileName);

                    using (FileStream fileStream = File.Create(outPath))
                    {
                        decStream.CopyTo(fileStream);
                    }
                }

                Console.WriteLine("Finished writing!");
            }
        }

        public static void ExtractPersona2Script(string filePath, string outputDir = null)
        {
            using (var binNode = NodeFactory.FromFile(filePath))
            {
                var po = binNode
                    .TransformWith<Formats.P2IS.PSP.BinToEBD>()
                    .TransformWith<Formats.P2IS.PSP.EBDToPo>()
                    .TransformWith<Po2Binary>();

                string dir = NormalizeOutputDir(outputDir);
                string outPath = Path.Combine(dir, Path.GetFileNameWithoutExtension(filePath) + ".po");

                Console.WriteLine($"Writing {Path.GetFileName(outPath)} in {dir}...");
                po.Stream.WriteTo(outPath);

                Console.WriteLine("Finished writing!");
            }
        }

        public static void ExtractPersona2ScriptFromBin(string filePath, string outputDir = null)
        {
            using (var binNode = NodeFactory.FromFile(filePath))
            {
                var container = binNode.TransformWith<Formats.P2IS.PSP.BinToContainer>();
                var textNode = container.Children.FirstOrDefault(n => n.Name == "8");

                if (textNode == null)
                {
                    Console.WriteLine("Error: Text Node not found in container!");
                    return;
                }

                var po = textNode
                    .TransformWith<Formats.P2IS.PSP.BinToEBD>()
                    .TransformWith<Formats.P2IS.PSP.EBDToPo>()
                    .TransformWith<Po2Binary>();

                string dir = NormalizeOutputDir(outputDir);
                string outPath = Path.Combine(dir, Path.GetFileNameWithoutExtension(filePath) + ".po");

                Console.WriteLine($"Writing {Path.GetFileName(outPath)} in {dir}...");
                po.Stream.WriteTo(outPath);

                Console.WriteLine("Finished writing!");
            }
        }

        public static void ImportPersona2Script(string poFilePath, string outputPath = null)
        {
            using (var poNode = NodeFactory.FromFile(poFilePath))
            {
                var ebd = poNode.TransformWith<Binary2Po>()
                    .TransformWith<Formats.P2IS.PSP.PoToEBD>();

                var newBinary = ebd.TransformWith<Formats.P2IS.PSP.EbdToBin>();

                string output = outputPath;

                if (string.IsNullOrEmpty(output))
                {
                    Console.Write("Write to file (leave empty for default): ");
                    output = Console.ReadLine()?.Trim('"');

                    if (string.IsNullOrEmpty(output))
                    {
                        string dir = Path.GetDirectoryName(poFilePath);
                        string fileName = Path.GetFileNameWithoutExtension(poFilePath);
                        output = Path.Combine(dir, fileName + "_new.bin");
                    }
                }

                Console.WriteLine($"Writing {Path.GetFileName(output)}...");
                newBinary.Stream.WriteTo(output);

                Console.WriteLine("Finished writing!");
            }
        }

        public static void ImportPersona2ScriptToBin(string poFilePath, string binFilePath, string outputPath = null)
        {
            using (var poNode = NodeFactory.FromFile(poFilePath))
            using (var binNode = NodeFactory.FromFile(binFilePath))
            {
                var newScriptBinary = poNode.TransformWith<Binary2Po>()
                    .TransformWith<Formats.P2IS.PSP.PoToEBD>()
                    .TransformWith<Formats.P2IS.PSP.EbdToBin>();

                var container = binNode.TransformWith<Formats.P2IS.PSP.BinToContainer>();
                var textNode = container.Children.FirstOrDefault(n => n.Name == "8");

                if (textNode == null)
                {
                    Console.WriteLine("Error: Text Node (8) not found in container!");
                    return;
                }

                Console.WriteLine("Found text node in container. Replacing...");
                textNode.ChangeFormat(newScriptBinary.Format);

                var finalBinary = container.TransformWith<Formats.P2IS.PSP.ContainerToBin>();

                string output = outputPath;

                if (string.IsNullOrEmpty(output))
                {
                    Console.Write("Write to file (leave empty for default): ");
                    output = Console.ReadLine()?.Trim('"');

                    if (string.IsNullOrEmpty(output))
                    {
                        string dir = Path.GetDirectoryName(binFilePath);
                        string fileName = Path.GetFileNameWithoutExtension(binFilePath);
                        output = Path.Combine(dir, fileName + "_patched.bin");
                    }
                }

                Console.WriteLine($"Writing {Path.GetFileName(output)}...");
                finalBinary.Stream.WriteTo(output);

                Console.WriteLine("Finished writing patched BIN!");
            }
        }

        public static void ImportPersona2Bin(string directoryPath)
        {
            string indexPath = Path.Combine(directoryPath, "index.txt");

            if (!File.Exists(indexPath))
            {
                Console.WriteLine("Error: index.txt not found in directory!");
                return;
            }

            var indexLines = File.ReadAllLines(indexPath);
            var fileEntries = new List<(int id, string filename)>();

            foreach (var line in indexLines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("Type:"))
                    continue;

                var parts = line.Split(':');
                if (parts.Length == 2)
                {
                    int id = int.Parse(parts[0].Trim());
                    string filename = parts[1].Trim();
                    fileEntries.Add((id, filename));
                }
            }

            if (fileEntries.Count == 0)
            {
                Console.WriteLine("Error: No file entries found in index.txt!");
                return;
            }

            Console.WriteLine($"Found {fileEntries.Count} files to pack");

            NodeContainerFormat container = new NodeContainerFormat();

            foreach (var (id, filename) in fileEntries.OrderBy(x => x.id))
            {
                string filePath = Path.Combine(directoryPath, filename);

                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Warning: File {filename} not found, skipping...");
                    continue;
                }

                Console.WriteLine($"Adding {filename}...");

                using (var fileNode = NodeFactory.FromFile(filePath))
                {
                    var dataStream = DataStreamFactory.FromMemory();
                    fileNode.Stream.Position = 0;
                    fileNode.Stream.WriteTo(dataStream);
                    dataStream.Position = 0;

                    container.Root.Add(new Node(id.ToString(), new BinaryFormat(dataStream)));
                }
            }

            var converter = new Formats.P2IS.PSP.ContainerToBin();
            var bin = converter.Convert(container);

            Console.Write("Write to file (leave empty for default): ");
            string output = Console.ReadLine()?.Trim('"');

            if (string.IsNullOrEmpty(output))
            {
                string parentDir = Path.GetDirectoryName(directoryPath);
                string folderName = Path.GetFileName(directoryPath);
                output = Path.Combine(parentDir, folderName + "_new.bin");
            }

            Console.WriteLine($"Writing {Path.GetFileName(output)}...");
            bin.Stream.WriteTo(output);

            Console.WriteLine("Finished writing BIN!");
        }

        public static void ImportPersona2GzBin(string directoryPath)
        {
            string indexPath = Path.Combine(directoryPath, "index.txt");

            if (!File.Exists(indexPath))
            {
                Console.WriteLine("Error: index.txt not found in directory!");
                return;
            }

            Console.WriteLine($"Reading index from: {indexPath}");
            var indexLines = File.ReadAllLines(indexPath);
            Console.WriteLine($"Total lines in index.txt: {indexLines.Length}");

            var fileEntries = new List<(int id, string filename)>();

            for (int i = 0; i < indexLines.Length; i++)
            {
                string line = indexLines[i];

                Console.WriteLine($"Line {i}: '{line}'");

                if (string.IsNullOrWhiteSpace(line))
                {
                    Console.WriteLine("  -> Skipped: empty line");
                    continue;
                }

                if (line.StartsWith("Type:"))
                {
                    Console.WriteLine("  -> Skipped: Type line");
                    continue;
                }

                var parts = line.Split(':');

                if (parts.Length != 2)
                {
                    Console.WriteLine($"  -> Skipped: Invalid format (expected 'id=filename', got {parts.Length} parts)");
                    continue;
                }

                if (int.TryParse(parts[0].Trim(), out int id))
                {
                    string filename = parts[1].Trim();
                    fileEntries.Add((id, filename));
                    Console.WriteLine($"  -> Added: ID={id}, File={filename}");
                }
                else
                {
                    Console.WriteLine($"  -> Skipped: Could not parse ID from '{parts[0]}'");
                }
            }

            if (fileEntries.Count == 0)
            {
                Console.WriteLine("\nError: No file entries found in index.txt!");
                Console.WriteLine("Expected format:");
                Console.WriteLine("Type: GZBIN");
                Console.WriteLine("0:file1.bin");
                Console.WriteLine("1:file2.bin");
                Console.WriteLine("...");
                return;
            }

            Console.WriteLine($"\nFound {fileEntries.Count} files to pack\n");

            NodeContainerFormat container = new NodeContainerFormat();

            foreach (var (id, filename) in fileEntries.OrderBy(x => x.id))
            {
                string filePath = Path.Combine(directoryPath, filename);

                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Warning: File {filename} not found, skipping...");
                    continue;
                }

                Console.WriteLine($"Compressing {filename}...");

                byte[] fileData = File.ReadAllBytes(filePath);

                using (var compressedStream = new MemoryStream())
                {
                    // Escribir header GZip manualmente con el nombre del archivo
                    WriteGZipHeader(compressedStream, filename);

                    // Comprimir los datos
                    using (var deflateStream = new DeflateStream(
                        compressedStream,
                        CompressionMode.Compress,
                        true))
                    {
                        deflateStream.Write(fileData, 0, fileData.Length);
                    }

                    // Escribir CRC32 y tamaño original
                    WriteGZipFooter(compressedStream, fileData);

                    compressedStream.Position = 0;
                    byte[] compressedData = compressedStream.ToArray();

                    var dataStream = DataStreamFactory.FromMemory();
                    dataStream.Write(compressedData, 0, compressedData.Length);
                    dataStream.Position = 0;

                    container.Root.Add(new Node(id.ToString(), new BinaryFormat(dataStream)));

                    Console.WriteLine($"  Original: {fileData.Length} bytes → Compressed: {compressedData.Length} bytes");
                }
            }

            var converter = new Formats.P2IS.PSP.ContainerToGzBin();
            var gzbin = converter.Convert(container);

            Console.Write("Write to file (leave empty for default): ");
            string output = Console.ReadLine()?.Trim('"');

            if (string.IsNullOrEmpty(output))
            {
                string parentDir = Path.GetDirectoryName(directoryPath);
                string folderName = Path.GetFileName(directoryPath);
                output = Path.Combine(parentDir, folderName + "_new.bin");
            }

            Console.WriteLine($"Writing {Path.GetFileName(output)}...");
            gzbin.Stream.WriteTo(output);

            Console.WriteLine("Finished writing GZBIN!");
        }

        private static void WriteGZipHeader(Stream stream, string filename)
        {
            // Magic number (1f 8b)
            stream.WriteByte(0x1f);
            stream.WriteByte(0x8b);

            // Compression method (08 = DEFLATE)
            stream.WriteByte(0x08);

            // Flags (FNAME = 0x08, indica que hay nombre de archivo)
            stream.WriteByte(0x08);

            // Timestamp (4 bytes) - usar Unix timestamp actual
            uint timestamp = (uint)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            stream.WriteByte((byte)(timestamp & 0xFF));
            stream.WriteByte((byte)((timestamp >> 8) & 0xFF));
            stream.WriteByte((byte)((timestamp >> 16) & 0xFF));
            stream.WriteByte((byte)((timestamp >> 24) & 0xFF));

            // Extra flags (02 = máxima compresión)
            stream.WriteByte(0x02);

            // OS (00 = FAT filesystem / 03 = Unix / 0B = NTFS)
            stream.WriteByte(0x00);

            // Nombre del archivo (null-terminated)
            byte[] filenameBytes = System.Text.Encoding.ASCII.GetBytes(filename);
            stream.Write(filenameBytes, 0, filenameBytes.Length);
            stream.WriteByte(0x00); // Null terminator
        }

        private static void WriteGZipFooter(Stream stream, byte[] originalData)
        {
            // CRC32 checksum (4 bytes, little-endian)
            uint crc32 = CalculateCRC32(originalData);
            stream.WriteByte((byte)(crc32 & 0xFF));
            stream.WriteByte((byte)((crc32 >> 8) & 0xFF));
            stream.WriteByte((byte)((crc32 >> 16) & 0xFF));
            stream.WriteByte((byte)((crc32 >> 24) & 0xFF));

            // Tamaño original (4 bytes, little-endian, módulo 2^32)
            uint size = (uint)(originalData.Length & 0xFFFFFFFF);
            stream.WriteByte((byte)(size & 0xFF));
            stream.WriteByte((byte)((size >> 8) & 0xFF));
            stream.WriteByte((byte)((size >> 16) & 0xFF));
            stream.WriteByte((byte)((size >> 24) & 0xFF));
        }

        private static uint CalculateCRC32(byte[] data)
        {
            uint crc = 0xFFFFFFFF;

            for (int i = 0; i < data.Length; i++)
            {
                crc ^= data[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 1) != 0)
                        crc = (crc >> 1) ^ 0xEDB88320;
                    else
                        crc >>= 1;
                }
            }

            return ~crc;
        }

        public static void ImportPersona2ScriptToGzBin(
    string poFilePath,
    string gzBinFilePath,
    int containerIndex,
    string outputPath = null)
        {
            using (var poNode = NodeFactory.FromFile(poFilePath))
            using (var gzBinNode = NodeFactory.FromFile(gzBinFilePath))
            {
                // PO -> EBD -> BIN
                var newScriptBinary = poNode.TransformWith<Binary2Po>()
                    .TransformWith<Formats.P2IS.PSP.PoToEBD>()
                    .TransformWith<Formats.P2IS.PSP.EbdToBin>();

                // GZBIN -> Container
                var gzContainer = gzBinNode.TransformWith<Formats.P2IS.PSP.GzBinToContainer>();

                var targetNode = Navigator.IterateNodes(gzContainer)
                    .FirstOrDefault(n => n.Name == containerIndex.ToString());

                if (targetNode == null)
                {
                    Console.WriteLine($"Error: Container node {containerIndex} not found in GZBIN!");
                    return;
                }

                // Ensure position is at start before parsing header/name
                targetNode.Stream.Position = 0;

                // Read original base header fields (mtime/xfl/os) to preserve them
                var (origMTime, origXfl, origOs) = ReadGZipBaseHeaderFields(targetNode.Stream);

                // Reset to start so ObtainFileNameFromGZip reads correctly
                targetNode.Stream.Position = 0;

                var fileName = DataStreamUtils.ObtainFileNameFromGZip(targetNode.Stream);
                Console.WriteLine($"Processing {fileName}...");

                // Reset to start so decompression reads correctly
                targetNode.Stream.Position = 0;

                var decompressedStream = DataStreamUtils.DecompressDataStreamToMemory(targetNode.Stream);
                var tempDataStream = DataStreamFactory.FromStream(decompressedStream);
                tempDataStream.Position = 0;

                var innerBinNode = new Node("temp", new BinaryFormat(tempDataStream));

                // BIN -> Container
                var innerContainer = innerBinNode.TransformWith<Formats.P2IS.PSP.BinToContainer>();

                var textNode = innerContainer.Children.FirstOrDefault(n => n.Name == "8");
                if (textNode == null)
                {
                    Console.WriteLine("Error: Text Node (8) not found in inner container!");
                    return;
                }

                Console.WriteLine("Replacing text node in inner container...");
                textNode.ChangeFormat(newScriptBinary.Format);

                // Container -> BIN
                var patchedInnerBin = innerContainer.TransformWith<Formats.P2IS.PSP.ContainerToBin>();

                patchedInnerBin.Stream.Position = 0;
                byte[] patchedBinData = new byte[patchedInnerBin.Stream.Length];
                patchedInnerBin.Stream.Read(patchedBinData, 0, patchedBinData.Length);

                // Re-compress to GZip while keeping FNAME (and preserving mtime/xfl/os from the original)
                byte[] compressedData;
                using (var compressedStream = new MemoryStream())
                {
                    // Manual GZip header with FNAME
                    WriteGZipHeaderPreserveFields(
                        compressedStream,
                        fileName,
                        origMTime,
                        origXfl,
                        origOs);

                    // DEFLATE payload
                    using (var deflateStream = new DeflateStream(
                        compressedStream,
                        CompressionMode.Compress,
                        true))
                    {
                        deflateStream.Write(patchedBinData, 0, patchedBinData.Length);
                    }

                    // Footer: CRC32 + ISIZE
                    WriteGZipFooter(compressedStream, patchedBinData);

                    compressedData = compressedStream.ToArray();
                }

                Console.WriteLine($"  Original: {patchedBinData.Length} bytes → Compressed: {compressedData.Length} bytes");

                var newDataStream = DataStreamFactory.FromMemory();
                newDataStream.Write(compressedData, 0, compressedData.Length);
                newDataStream.Position = 0;

                targetNode.ChangeFormat(new BinaryFormat(newDataStream));

                // Container -> GZBIN
                var finalGzBin = gzContainer.TransformWith<Formats.P2IS.PSP.ContainerToGzBin>();

                string output = outputPath;

                if (string.IsNullOrEmpty(output))
                {
                    Console.Write("Write to file (leave empty for default): ");
                    output = Console.ReadLine()?.Trim('"');

                    if (string.IsNullOrEmpty(output))
                    {
                        string dir = Path.GetDirectoryName(gzBinFilePath);
                        string fileName2 = Path.GetFileNameWithoutExtension(gzBinFilePath);
                        output = Path.Combine(dir, fileName2 + "_patched.bin");
                    }
                }

                Console.WriteLine($"Writing {Path.GetFileName(output)}...");
                finalGzBin.Stream.WriteTo(output);

                Console.WriteLine("Finished writing patched GZBIN!");
            }
        }

        private static (uint mtime, byte xfl, byte os) ReadGZipBaseHeaderFields(DataStream stream)
        {
            long oldPos = stream.Position;
            try
            {
                stream.Position = 0;

                // GZip fixed header is 10 bytes
                byte[] hdr = new byte[10];
                int read = stream.Read(hdr, 0, hdr.Length);

                // Minimal validation: ID1 ID2 CM (DEFLATE)
                if (read < 10 || hdr[0] != 0x1F || hdr[1] != 0x8B || hdr[2] != 0x08)
                {
                    // Fallback defaults if this doesn't look like a valid gzip stream
                    return (0u, 0x00, 0x00);
                }

                // MTIME is a 32-bit little-endian Unix timestamp
                uint mtime =
                    (uint)(hdr[4]
                    | (hdr[5] << 8)
                    | (hdr[6] << 16)
                    | (hdr[7] << 24));

                // XFL and OS are single bytes at offsets 8 and 9
                byte xfl = hdr[8];
                byte os = hdr[9];

                return (mtime, xfl, os);
            }
            finally
            {
                stream.Position = oldPos;
            }
        }

        private static void WriteGZipHeaderPreserveFields(Stream stream, string filename, uint mtime, byte xfl, byte os)
        {
            // ID1 ID2 (gzip magic)
            stream.WriteByte(0x1f);
            stream.WriteByte(0x8b);

            // CM = 08 (DEFLATE)
            stream.WriteByte(0x08);

            // FLG = 08 (FNAME present)
            stream.WriteByte(0x08);

            // MTIME (little-endian)
            stream.WriteByte((byte)(mtime & 0xFF));
            stream.WriteByte((byte)((mtime >> 8) & 0xFF));
            stream.WriteByte((byte)((mtime >> 16) & 0xFF));
            stream.WriteByte((byte)((mtime >> 24) & 0xFF));

            // XFL, OS
            stream.WriteByte(xfl);
            stream.WriteByte(os);

            // FNAME (ASCII, null-terminated)
            byte[] filenameBytes = System.Text.Encoding.ASCII.GetBytes(filename);
            stream.Write(filenameBytes, 0, filenameBytes.Length);
            stream.WriteByte(0x00);
        }
    }
}
