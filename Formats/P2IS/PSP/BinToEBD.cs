using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Yarhl.FileFormat;
using Yarhl.IO;
using Yarhl.Media.Text.Encodings;
using ClassicPersonaToolkit.Helpers.P2IS.PSP.Models;

namespace ClassicPersonaToolkit.Formats.P2IS.PSP
{
    public class BinToEBD : IConverter<BinaryFormat, EBD>
    {
        private const ushort EntryMarker = 0x1431;
        private const ushort EntryEnd = 0x1103;
        private const int MarkerSkipBytes = 0x6;


        // Credits to eiowlta for the control codes
        private static readonly IReadOnlyDictionary<byte, string> ControlCodes = new Dictionary<byte, string>
        {
            { 0x01, "NL" },
            { 0x02, "CLEAR" },
            { 0x03, "END" },
            { 0x05, "DELAY" },
            { 0x06, "WAIT" },
            { 0x07, "SYNC" },
            { 0x08, "CHOICE" },
            { 0x09, "END_CHOICE" },
            { 0x0E, "VAR" },
            { 0x10, "IF" },
            { 0x11, "IF_NOT" },
            { 0x12, "LNAME" },
            { 0x13, "FNAME" },
            { 0x14, "NNAME" },
            { 0x1F, "DBL_TAB" },
            { 0x20, "SPACE" },
            { 0x21, "HALF_TAB" },
        };

        private static readonly IReadOnlyDictionary<byte, string> ExtendedCodes = new Dictionary<byte, string>
        {
            { 0x31, "COLOR" },
            { 0x32, "SYM" },
            { 0x33, "ITEM_WITH_TYPE" },
        };

        private static readonly IReadOnlyDictionary<byte, string> ColorNames = new Dictionary<byte, string>
        {
            { 0x01, "WHITE_1" },
            { 0x05, "WHITE_5" },
            { 0x07, "LIGHT_BLUE" },
            { 0x0B, "LIME" },
            { 0x0D, "ORANGE" },
            { 0x14, "YELLOW" },
            { 0x15, "TEAL" },
            { 0x16, "FUCHSIA" },
            { 0x17, "LIGHT_GRAY" },
            { 0x18, "GREEN" },
            { 0x19, "BLACK" },
            { 0x20, "BLUE" },
            { 0x21, "PINK" },
            { 0x32, "NAME_GREEN" },
            { 0x33, "RANK" },
            { 0x13, "LIME_2" },
            { 0x11, "WHITE_1_ALT" },
            { 0x12, "WHITE" },
            { 0x02, "DEFAULT" },
        };

        private static readonly IReadOnlyDictionary<byte, string> SymbolNames = new Dictionary<byte, string>
        {
            { 0x01, "UNK_01" },
            { 0x02, "UNK_02" },
            { 0x03, "UNK_03" },
            { 0x04, "UNK_04" },
            { 0x05, "UNK_05" },
            { 0x06, "UNK_06" },
            { 0x07, "HEART" },
            { 0x08, "UNK_08" },
            { 0x09, "UNK_09" },
            { 0x0A, "UNK_0A" },
            { 0x0B, "UNK_0B" },
            { 0x0C, "UNK_0C" },
            { 0x0D, "UNK_0D" },
            { 0x0E, "UNK_0E" },
            { 0x0F, "UNK_0F" },
            { 0x10, "UNK_10" },
            { 0x11, "UNK_11" },
            { 0x12, "UNK_12" },
            { 0x13, "LQUOTE" },
            { 0x14, "RQUOTE" },
            { 0x15, "UNK_15" },
        };

        private static readonly IReadOnlyDictionary<byte, string> SpecialChars = new Dictionary<byte, string>
        {
            { 0x01, "\n" },
            { 0x20, " " }
        };

        public EBD Convert(BinaryFormat source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var reader = new DataReader(source.Stream)
            {
                DefaultEncoding = new EscapeOutRangeEncoding(Encoding.Unicode),
            };

            var ebd = new EBD();

            ReadHeader(reader, ebd);
            ValidateHeader(reader, ebd);
            ExtractStartToStringData(reader, ebd);
            ParseEntries(reader, ebd);

            return ebd;
        }

        private static void ReadHeader(DataReader reader, EBD ebd)
        {
            ebd.Header.StartPos = reader.ReadUInt32();
            ebd.Header.FunctionsPos = reader.ReadUInt32();
            ebd.Header.NumberOfFunctions = reader.ReadUInt32();
            ebd.Header.InstructionsPos = reader.ReadUInt32();
            ebd.Header.ArgumentsPos = reader.ReadUInt32();
            ebd.Header.StringDataPos = reader.ReadUInt32();
        }

        private static void ValidateHeader(DataReader reader, EBD ebd)
        {
            if (ebd.Header.StringDataPos >= reader.Stream.Length)
                throw new InvalidDataException("StringDataPos is outside the stream.");

            if (ebd.Header.StringDataPos < ebd.Header.StartPos)
                throw new InvalidDataException("StringDataPos is before StartPos.");
        }

        private static void ExtractStartToStringData(DataReader reader, EBD ebd)
        {
            long originalPosition = reader.Stream.Position;

            reader.Stream.Position = ebd.Header.StartPos;
            int blockLength = checked((int)(ebd.Header.StringDataPos - ebd.Header.StartPos));
            byte[] blockData = reader.ReadBytes(blockLength);
            ebd.AdditionalData["StartToStringData"] = blockData;

            reader.Stream.Position = originalPosition;
        }

        private static void ParseEntries(DataReader reader, EBD ebd)
        {
            reader.Stream.Position = ebd.Header.StringDataPos;

            uint id = 0;

            while (TryReadUInt16(reader, out ushort word))
            {
                if (word != EntryMarker)
                    continue;

                long markerOffset = reader.Stream.Position - 2;

                if (!EnsureAvailable(reader, MarkerSkipBytes))
                    break;

                reader.Stream.Position += MarkerSkipBytes;

                string text = ReadTextUntilTerminator(reader);

                ebd.Entries.Add(new EBD.Entry
                {
                    Id = id++,
                    Offset = markerOffset,
                    Text = text
                });
            }
        }

        private static string ReadTextUntilTerminator(DataReader reader)
        {
            var sb = new StringBuilder();

            while (TryReadUInt16(reader, out ushort buffer))
            {
                if (buffer == EntryEnd || buffer == EntryMarker)
                    break;

                byte upper = (byte)(buffer >> 8);
                byte lower = (byte)(buffer & 0xFF);

                if (!TryAppendToken(reader, sb, upper, lower, buffer))
                    break;
            }

            return sb.ToString();
        }

        private static bool TryAppendToken(DataReader reader, StringBuilder sb, byte upper, byte lower, ushort raw)
        {
            switch (upper)
            {
                case 0x14:
                    return TryAppendExtended(reader, sb, lower);

                case 0x12:
                    return TryAppendControlWithArg(reader, sb, lower);

                case 0x11:
                    AppendSimpleControl(sb, lower);
                    return true;

                default:
                    sb.Append((char)raw);
                    return true;
            }
        }

        private static byte SwapNibbles(byte value)
        {
            return (byte)(((value & 0x0F) << 4) | ((value & 0xF0) >> 4));
        }

        // 0x14XX: [opcode][0x0000][0x0000][value]
        private static bool TryAppendExtended(DataReader reader, StringBuilder sb, byte opcode)
        {
            if (!EnsureAvailable(reader, 6))
                return false;

            _ = reader.ReadUInt16();
            _ = reader.ReadUInt16();
            ushort val = reader.ReadUInt16();

            if (!ExtendedCodes.TryGetValue(opcode, out string codeName))
            {
                sb.Append("{UNK14");
                sb.Append(opcode.ToString("X2"));
                sb.Append(":");
                sb.Append(val);
                sb.Append("}");
                return true;
            }

            if (codeName == "COLOR")
            {
                byte colorValue = (byte)(val & 0xFF);
                string colorName = ColorNames.TryGetValue(colorValue, out string cn)
                    ? cn
                    : "UNK_" + colorValue.ToString("X2");

                sb.Append("{COLOR:");
                sb.Append(colorName);
                sb.Append("}");
                return true;
            }

            if (codeName == "SYM")
            {
                byte symValue = (byte)(val & 0xFF);

                // Some files store the symbol as swapped-nibble already.
                // Prefer the swapped value if it is known, otherwise fall back to the raw value.
                byte swapped = SwapNibbles(symValue);
                byte lookup = SymbolNames.ContainsKey(swapped) ? swapped : symValue;

                string symName = SymbolNames.TryGetValue(lookup, out string sn)
                    ? sn
                    : "UNK_" + lookup.ToString("X2");

                if (symName == "LQUOTE")
                    sb.Append('“');
                else if (symName == "RQUOTE")
                    sb.Append('”');
                else
                {
                    sb.Append("{SYM:");
                    sb.Append(symName);
                    sb.Append("}");
                }

                return true;
            }

            if (codeName == "ITEM_WITH_TYPE")
            {
                sb.Append("{ITEM_WITH_TYPE:");
                sb.Append(val);
                sb.Append("}");
                return true;
            }

            sb.Append("{");
            sb.Append(codeName);
            sb.Append(":");
            sb.Append(val);
            sb.Append("}");
            return true;
        }

        // 0x12XX: [opcode][arg]
        private static bool TryAppendControlWithArg(DataReader reader, StringBuilder sb, byte opcode)
        {
            if (!EnsureAvailable(reader, 2))
                return false;

            ushort arg = reader.ReadUInt16();

            if (ControlCodes.TryGetValue(opcode, out string code))
            {
                sb.Append("{");
                sb.Append(code);
                sb.Append(":");
                sb.Append(arg);
                sb.Append("}");
            }
            else
            {
                sb.Append("{UNK");
                sb.Append(opcode.ToString("X2"));
                sb.Append(":");
                sb.Append(arg);
                sb.Append("}");
            }

            return true;
        }

        // 0x11XX: [opcode]
        private static void AppendSimpleControl(StringBuilder sb, byte opcode)
        {
            if (SpecialChars.TryGetValue(opcode, out string special))
            {
                sb.Append(special);
                return;
            }

            if (ControlCodes.TryGetValue(opcode, out string code))
            {
                sb.Append("{");
                sb.Append(code);
                sb.Append("}");
                return;
            }

            sb.Append("{UNK");
            sb.Append(opcode.ToString("X2"));
            sb.Append("}");
        }

        private static bool TryReadUInt16(DataReader reader, out ushort value)
        {
            if (!EnsureAvailable(reader, 2))
            {
                value = 0;
                return false;
            }

            value = reader.ReadUInt16();
            return true;
        }

        private static bool EnsureAvailable(DataReader reader, int bytes)
        {
            return reader.Stream.Position + bytes <= reader.Stream.Length;
        }
    }
}
