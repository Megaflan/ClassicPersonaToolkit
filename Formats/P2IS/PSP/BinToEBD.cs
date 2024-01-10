using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarhl.FileFormat;
using Yarhl.IO;
using Yarhl.Media.Text.Encodings;
using ClassicPersonaToolkit.Helpers.P2IS.PSP.Models;
using System.Net.Sockets;

namespace ClassicPersonaToolkit.Formats.P2IS.PSP
{
    public class BinToEBD : IConverter<BinaryFormat, EBD>
    {
        Dictionary<byte, string> controlCodes = new Dictionary<byte, string>
        {
            { 0x05, "WAIT" },
            { 0x12, "FNAME" },
            { 0x13, "LNAME" },
        };

        Dictionary<byte, string> specialChars = new Dictionary<byte, string>
        {
            { 0x01, "\n" },
            { 0x20, " " }
        };

        public EBD Convert(BinaryFormat source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            DataReader reader = new DataReader(source.Stream)
            {
                DefaultEncoding = new EscapeOutRangeEncoding(Encoding.Unicode),
            };

            EBD ebd = new EBD();

            reader.Stream.Position = 0xC; // Let's jump to 0xC for now.

            long functionInstructionsPosition = reader.ReadUInt32();
            long instructionsArgumentsPosition = reader.ReadUInt32();
            long stringDataPosition = reader.ReadUInt32();

            //We are going to skip to StringData for now.
            reader.Stream.Position = stringDataPosition;

            uint id = 0;
            while (reader.Stream.Position < reader.Stream.Length - 1)
            {
                long offset = reader.Stream.Position;
                ushort value = reader.ReadUInt16();

                if (value == 0x1431)
                {
                    reader.Stream.Position += 0x6; //Skip null padding
                    ushort buffer = 0;
                    StringBuilder sb = new StringBuilder();

                    while (buffer != 0x1103)
                    {
                        buffer = reader.ReadUInt16();

                        if (buffer == 0x1431)
                            break;

                        if ((byte)(buffer >> 12) == 1) //If the first 4 bits are 0001, it's a control code.
                        {
                            var argument = (byte)((buffer >> 8) & 0x0F); //Extract the second group, this defines the arguments.
                            var type = (byte)(buffer & 0xFF); //Extract the third and four group, this defines the control code type.

                            if (specialChars.ContainsKey(type))
                                sb.Append(specialChars[type]);
                            else
                            {
                                sb.Append("{");
                                sb.Append(controlCodes.ContainsKey(type) ? controlCodes[type] : "UNK" + type);
                                sb.Append(":");
                                sb.Append(argument);
                                sb.Append("}");
                            }                            
                        }
                        else
                            sb.Append(((char)buffer).ToString());
                    }

                    ebd.Entries.Add(new EBD.Entry()
                    {
                        Id = id,
                        Offset = offset,
                        Text = sb.ToString(),
                    });
                    id++;
                }
            }



            return ebd;
        }
    }
}
