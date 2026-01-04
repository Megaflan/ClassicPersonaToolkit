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
    public class ContainerToGzBin : IConverter<NodeContainerFormat, BinaryFormat>
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
            List<uint> pointers = new List<uint>();

            uint currentPosition = (uint)((nodes.Count + 1) * 4);

            foreach (var node in nodes)
            {
                var binary = node.GetFormatAs<BinaryFormat>();

                Console.WriteLine($"Node {node.Name}:");
                Console.WriteLine($"  Pointer: 0x{currentPosition:X8}");
                Console.WriteLine($"  Size: 0x{binary.Stream.Length:X8}");

                writer.Write(currentPosition);
                pointers.Add(currentPosition);
                currentPosition += (uint)binary.Stream.Length;
            }

            writer.Write((uint)0);

            foreach (var node in nodes)
            {
                var binary = node.GetFormatAs<BinaryFormat>();
                binary.Stream.Position = 0;
                binary.Stream.WriteTo(writer.Stream);
            }

            return new BinaryFormat(stream);
        }
    }
}
