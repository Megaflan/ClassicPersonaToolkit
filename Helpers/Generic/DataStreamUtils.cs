using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarhl.FileSystem;
using Yarhl.IO;

namespace ClassicPersonaToolkit.Helpers.Generic
{
    public static class DataStreamUtils
    {
        public static GZipStream ConvertDataStreamToGZipStream(DataStream dataStream)
        {
            var buffer = new byte[(int)dataStream.Length];
            dataStream.Read(buffer, 0, (int)dataStream.Length);
            MemoryStream stream = new MemoryStream(buffer);
            stream.Seek(0, SeekOrigin.Begin);

            return new GZipStream(stream, CompressionMode.Decompress);
        }

        public static string ObtainFileNameFromGZip(DataStream stream)
        {
            var dr = new DataReader(stream)
            {
                DefaultEncoding = new UTF8Encoding(),
                Endianness = EndiannessMode.LittleEndian,
            };

            dr.Stream.Seek(0x0, SeekMode.Start);
            if (dr.ReadUInt16() != 0x8b1f)
                return string.Empty;
            else
            {
                dr.Stream.Seek(0xA, SeekMode.Start);
                var fileName = dr.ReadString();
                dr.Stream.Seek(0x0, SeekMode.Start);

                return fileName;
            }
        }
    }
}
