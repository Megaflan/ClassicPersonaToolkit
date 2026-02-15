using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Yarhl.IO;

namespace ClassicPersonaToolkit.Helpers.Generic
{
    public static class DataStreamUtils
    {
        private static void SkipExactly(DataStream dataStream, int count)
        {
            if (count <= 0) return;

            byte[] buf = new byte[4096];
            int remaining = count;
            while (remaining > 0)
            {
                int toRead = Math.Min(remaining, buf.Length);
                int read = dataStream.Read(buf, 0, toRead);
                if (read <= 0)
                    throw new EndOfStreamException("Truncated GZip stream.");
                remaining -= read;
            }
        }

        public static GZipStream ConvertDataStreamToGZipStream(DataStream dataStream)
        {
            var buffer = new byte[(int)dataStream.Length];
            dataStream.Position = 0;
            dataStream.Read(buffer, 0, buffer.Length);

            var stream = new MemoryStream(buffer);
            stream.Position = 0;

            return new GZipStream(stream, CompressionMode.Decompress);
        }

        public static MemoryStream DecompressDataStreamToMemory(DataStream dataStream)
        {
            byte[] input = new byte[(int)dataStream.Length];
            dataStream.Position = 0;
            dataStream.Read(input, 0, input.Length);

            using var inMem = new MemoryStream(input, writable: false);
            using var gzip = new GZipStream(inMem, CompressionMode.Decompress);

            var outMem = new MemoryStream();
            gzip.CopyTo(outMem);
            outMem.Position = 0;
            return outMem;
        }

        public static MemoryStream CompressDataStreamToGZipWithFileName(DataStream dataStream, string fileName, CompressionLevel level = CompressionLevel.Optimal)
        {
            if (dataStream == null)
                throw new ArgumentNullException(nameof(dataStream));
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));

            byte[] input = new byte[(int)dataStream.Length];
            dataStream.Position = 0;
            dataStream.Read(input, 0, input.Length);

            byte[] gz;
            using (var outMem = new MemoryStream())
            {
                using (var gzip = new GZipStream(outMem, level, leaveOpen: true))
                {
                    gzip.Write(input, 0, input.Length);
                }
                gz = outMem.ToArray();
            }

            byte[] patched = AddFileNameToGzipHeader(gz, fileName, Encoding.UTF8);
            return new MemoryStream(patched, writable: false);
        }

        public static byte[] AddFileNameToGzipHeader(byte[] gzipBytes, string fileName, Encoding encoding)
        {
            if (gzipBytes == null)
                throw new ArgumentNullException(nameof(gzipBytes));
            if (fileName == null)
                throw new ArgumentNullException(nameof(fileName));
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));
            if (gzipBytes.Length < 10)
                throw new InvalidDataException("GZip is too short.");

            if (gzipBytes[0] != 0x1F || gzipBytes[1] != 0x8B || gzipBytes[2] != 0x08)
                throw new InvalidDataException("Invalid GZip header (expected 1F 8B 08).");

            byte flg = gzipBytes[3];
            if ((flg & 0x08) != 0)
                return gzipBytes;

            byte[] nameBytes = encoding.GetBytes(fileName);
            int insertLen = nameBytes.Length + 1;

            byte[] result = new byte[gzipBytes.Length + insertLen];

            Buffer.BlockCopy(gzipBytes, 0, result, 0, 10);
            result[3] = (byte)(result[3] | 0x08);

            Buffer.BlockCopy(nameBytes, 0, result, 10, nameBytes.Length);
            result[10 + nameBytes.Length] = 0x00;

            Buffer.BlockCopy(gzipBytes, 10, result, 10 + insertLen, gzipBytes.Length - 10);

            return result;
        }

        public static string ObtainFileNameFromGZip(DataStream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (stream.Length < 10)
                return string.Empty;

            stream.Position = 0;

            int id1 = stream.ReadByte();
            int id2 = stream.ReadByte();
            int cm = stream.ReadByte();
            int flg = stream.ReadByte();

            if (id1 != 0x1F || id2 != 0x8B || cm != 0x08)
            {
                stream.Position = 0;
                return string.Empty;
            }

            SkipExactly(stream, 6);

            if ((flg & 0x04) != 0)
            {
                int xlenLo = stream.ReadByte();
                int xlenHi = stream.ReadByte();
                if (xlenLo < 0 || xlenHi < 0)
                {
                    stream.Position = 0;
                    return string.Empty;
                }

                int xlen = xlenLo | (xlenHi << 8);
                SkipExactly(stream, xlen);
            }

            string fileName = string.Empty;

            if ((flg & 0x08) != 0)
            {
                var bytes = new List<byte>(64);
                while (true)
                {
                    int b = stream.ReadByte();
                    if (b < 0) break;
                    if (b == 0) break;
                    bytes.Add((byte)b);
                }

                fileName = Encoding.UTF8.GetString(bytes.ToArray());
            }

            if ((flg & 0x10) != 0)
            {
                while (true)
                {
                    int b = stream.ReadByte();
                    if (b < 0) break;
                    if (b == 0) break;
                }
            }

            if ((flg & 0x02) != 0)
            {
                SkipExactly(stream, 2);
            }

            stream.Position = 0;
            return fileName;
        }

        public static void GenerateIndex(Dictionary<int, string> fileList, string contType, string filePath)
        {
            using (StreamWriter writer = new StreamWriter(filePath + Path.DirectorySeparatorChar + "index.txt"))
            {
                writer.WriteLine("Type: " + contType);
                foreach (var f in fileList)
                {
                    if (f.Value != string.Empty)
                        writer.WriteLine(f.Key + ":" + f.Value);
                }
            }
        }        
    }
}
