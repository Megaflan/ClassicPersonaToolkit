using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarhl.FileFormat;
using Yarhl.FileSystem;
using Yarhl.IO;

namespace ClassicPersonaToolkit.Formats.P2IS.PSP
{
    public class ContainerToBin : IConverter<NodeContainerFormat, BinaryFormat>
    {
        public BinaryFormat Convert(NodeContainerFormat source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            DataStream stream = DataStreamFactory.FromMemory();
            DataWriter writer = new DataWriter(stream)
            {
                Endianness = EndiannessMode.LittleEndian
            };

            var nodes = source.Root.Children.ToList();

            writer.Write((uint)nodes.Count);

            foreach (var node in nodes)
            {
                var binary = node.GetFormatAs<BinaryFormat>();
                writer.Write((uint)binary.Stream.Length);
            }

            while (writer.Stream.Position % 16 != 0)
            {
                writer.Write((byte)0);
            }

            foreach (var node in nodes)
            {
                var binary = node.GetFormatAs<BinaryFormat>();
                binary.Stream.Position = 0;
                binary.Stream.WriteTo(writer.Stream);

                long currentSize = binary.Stream.Length;
                long alignedSize = ((currentSize + 0xF) >> 4) << 4;
                long paddingNeeded = alignedSize - currentSize;

                for (int i = 0; i < paddingNeeded; i++)
                {
                    writer.Write((byte)0);
                }
            }

            return new BinaryFormat(stream);
        }
    }
}
