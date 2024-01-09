using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Yarhl.FileFormat;
using Yarhl.FileSystem;
using Yarhl.IO;

namespace ClassicPersonaToolkit.Formats.P1.PSP
{
    public class BinToContainer : IConverter<BinaryFormat, NodeContainerFormat>
    {
        public NodeContainerFormat Convert(BinaryFormat source)
        {
            NodeContainerFormat file = new NodeContainerFormat();
            var dr = new DataReader(source.Stream)
            {
                DefaultEncoding = new UTF8Encoding(),
                Endianness = EndiannessMode.LittleEndian,
            };

            uint fileCount = dr.ReadUInt32();
            List<uint> fileSizes = new List<uint>();            

            for (int i = 0; i < fileCount; i++)
            {
                fileSizes.Add(dr.ReadUInt32());
            }

            dr.SkipPadding(0x10);

            long position = dr.Stream.Position;
            for (int i = 0; i < fileCount; i++)
            {
                file.Root.Add(new Node($"{i}", new BinaryFormat(source.Stream, position, fileSizes[i])));
                position += ((fileSizes[i] + 0xF) >> 4) << 4;

            }

            return file;
        }
    }
}
