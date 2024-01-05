using System.Text;
using Yarhl.FileFormat;
using Yarhl.FileSystem;
using Yarhl.IO;
using ClassicPersonaToolkit.Helpers.Generic;

namespace ClassicPersonaToolkit.Formats.P2IS.PSP
{
    public class GzBinToContainer : IConverter<BinaryFormat, NodeContainerFormat>
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

            uint rPos = 0xCAFE;
            int c = 0;
            uint prevPos = 0;
            while (rPos != 0x0)
            {                
                rPos = dr.ReadUInt32();
                if (rPos != prevPos)
                {
                    nodeList.Add(new NodeData(c, rPos, 0));
                    c++;                    
                }
                prevPos = rPos;
            }
            nodeList.RemoveAt(nodeList.Count - 1);

            for (int i = 0; i < nodeList.Count - 1; i++)
            {
                NodeData nodeA = nodeList[i];
                NodeData nodeB = nodeList[i + 1];

                nodeA.Size = nodeB.Position - nodeA.Position;
            }

            foreach (var node in nodeList)
            {
                file.Root.Add(new Node($"e{node.Id}", new BinaryFormat(source.Stream, node.Position, node.Size)));
            }
            
            return file;
        }
    }
}
