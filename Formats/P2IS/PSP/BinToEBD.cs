﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarhl.FileFormat;
using Yarhl.IO;
using Yarhl.Media.Text.Encodings;
using ClassicPersonaToolkit.Helpers.P2IS.PSP.Models;

namespace ClassicPersonaToolkit.Formats.P2IS.PSP
{
    public class BinToEBD : IConverter<BinaryFormat, EBD>
    {
        Dictionary<ushort, string> controlCodes = new Dictionary<ushort, string>
        {
            { 0x1101, "\n" },
            { 0x1102, "{1102}" },
            { 0x1103, "{1103}" },
            { 0x1104, "{1104}" },
            { 0x1105, "{1105}" },
            { 0x1106, "{1106}" },
            { 0x1107, "{1107}" },
            { 0x1108, "{1108}" },
            { 0x1109, "{1109}" },
            { 0x1110, "{1110}" },
            { 0x1112, "{FIRST_NAME}" },
            { 0x1113, "{LAST_NAME}" },
            { 0x1205, "{1205}" },
            { 0x1120, " " }
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
                        sb.Append(controlCodes.ContainsKey(buffer) ? controlCodes[buffer] : ((char)buffer).ToString());
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
