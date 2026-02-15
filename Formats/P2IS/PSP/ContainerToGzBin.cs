using System;
using System.Linq;
using Yarhl.FileFormat;
using Yarhl.FileSystem;
using Yarhl.IO;

namespace ClassicPersonaToolkit.Formats.P2IS.PSP
{
    public class ContainerToGzBin : IConverter<NodeContainerFormat, BinaryFormat>
    {
        private const uint Alignment = 0x1000;

        public BinaryFormat Convert(NodeContainerFormat source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            DataStream stream = DataStreamFactory.FromMemory();
            var writer = new DataWriter(stream)
            {
                Endianness = EndiannessMode.LittleEndian
            };

            var nodes = source.Root.Children.ToList();

            uint headerEntries = nodes.Count == 0 ? 1u : (uint)(2 * nodes.Count);
            uint headerSize = headerEntries * sizeof(uint);

            uint currentPosition = AlignUp(headerSize, Alignment);

            for (int i = 0; i < nodes.Count; i++)
            {
                var binary = nodes[i].GetFormatAs<BinaryFormat>();
                uint size = (uint)binary.Stream.Length;

                Console.WriteLine($"Node {nodes[i].Name}:");
                Console.WriteLine($"  Pointer: 0x{currentPosition:X8}");
                Console.WriteLine($"  Size:    0x{size:X8}");

                if (i == 0)
                {
                    // First not repeated
                    writer.Write(currentPosition);
                }
                else
                {
                    // The rest, repeated
                    writer.Write(currentPosition);
                    writer.Write(currentPosition);
                }

                currentPosition = AlignUp(currentPosition + size, Alignment);
            }

            PadToAlignment(writer, Alignment);

            foreach (var node in nodes)
            {
                PadToAlignment(writer, Alignment);

                var binary = node.GetFormatAs<BinaryFormat>();
                binary.Stream.Position = 0;
                binary.Stream.WriteTo(writer.Stream);
            }

            return new BinaryFormat(stream);
        }

        private static uint AlignUp(uint value, uint alignment)
        {
            uint mask = alignment - 1;
            return (value + mask) & ~mask;
        }

        private static void PadToAlignment(DataWriter writer, uint alignment)
        {
            long pos = writer.Stream.Position;
            long aligned = (long)AlignUp((uint)pos, alignment);
            long pad = aligned - pos;
            if (pad <= 0) return;

            byte[] zeros = new byte[4096];
            while (pad > 0)
            {
                int chunk = (int)Math.Min(pad, zeros.Length);
                writer.Stream.Write(zeros, 0, chunk);
                pad -= chunk;
            }
        }
    }
}
