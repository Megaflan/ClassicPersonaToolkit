using System.Text;
using Yarhl.FileFormat;
using Yarhl.FileSystem;
using Yarhl.IO;
using ClassicPersonaToolkit.Helpers.Generic;

namespace ClassicPersonaToolkit.Formats.P2IS.PSP
{
    public class BinToContainer : IConverter<BinaryFormat, NodeContainerFormat>
    {
        List<NodeData> nodeList = new List<NodeData>();
        public NodeContainerFormat Convert(BinaryFormat source)
        {
            NodeContainerFormat file = new NodeContainerFormat();
            var dr = new DataReader(source.Stream)
            {
                DefaultEncoding = new UTF8Encoding(),
                Endianness = EndiannessMode.LittleEndian,
            };

            uint nodeQty = dr.ReadUInt32();
            for (int i = 0; i < nodeQty; i++)
            {
                uint size = dr.ReadUInt32();
                nodeList.Add(new NodeData(i, 0, size));
            }

            long padding = 0;
            while (dr.ReadInt32() == 0)
            {
                padding = dr.Stream.Position;
            }

            foreach (var node in nodeList)
            {
                node.Position += padding;
                file.Root.Add(new Node($"{node.Id}", new BinaryFormat(source.Stream, node.Position, node.Size)));
                padding += node.Size;
            }

            return file;
        }
    }
}
