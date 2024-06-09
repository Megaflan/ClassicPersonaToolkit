using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Yarhl.FileFormat;
using Yarhl.FileSystem;
using Yarhl.IO;

namespace ClassicPersonaToolkit.Formats.P1.PSP
{
    public class P1CV2ToContainer : IConverter<BinaryFormat, NodeContainerFormat>
    {
        public NodeContainerFormat Convert(BinaryFormat source)
        {
            NodeContainerFormat file = new NodeContainerFormat();
            var dr = new DataReader(source.Stream)
            {
                DefaultEncoding = new UTF8Encoding(),
                Endianness = EndiannessMode.LittleEndian,
            };

            List<uint> filePositions = new List<uint>();

            while (dr.Stream.Position < dr.Stream.Length)
            {
                uint value = dr.ReadUInt32();
                if (value == 0x00000000)
                    break;
                filePositions.Add(value);
            }

            filePositions = filePositions.Distinct().ToList();

            for (int i = 0; i < filePositions.Count; i++)
            {
                file.Root.Add(new Node($"{i}", new BinaryFormat(source.Stream, filePositions[i], ((i + 1 < filePositions.Count) ? filePositions[i + 1] : dr.Stream.Length) - filePositions[i])));
            }

            return file;
        }
    }
}
