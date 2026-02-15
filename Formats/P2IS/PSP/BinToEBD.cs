using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Yarhl.FileFormat;
using Yarhl.IO;
using Yarhl.Media.Text.Encodings;
using ClassicPersonaToolkit.Helpers.P2IS.PSP.Models;

namespace ClassicPersonaToolkit.Formats.P2IS.PSP
{
    public sealed class BinToEBD : IConverter<BinaryFormat, EBD>
    {
        private const ushort EntryEnd = 0x1103;
        private const int InstructionTextReference = 0x13;

        private const byte PrefixExtended = 0x14;        // 0x14XX
        private const byte PrefixControlWithArg = 0x12;  // 0x12XX
        private const byte PrefixSimpleControl = 0x11;   // 0x11XX

        private const string CharMapFileName = "char_map.csv";

        private static readonly Lazy<IReadOnlyDictionary<ushort, string>> CharMap =
            new Lazy<IReadOnlyDictionary<ushort, string>>(LoadCharMapNextToExe, isThreadSafe: true);

        // Credits to eiowlta for the control codes
        #region Control Codes
        private static readonly IReadOnlyDictionary<byte, string> ControlCodes = new Dictionary<byte, string>
        {
            [0x01] = "NL",
            [0x02] = "CLEAR",
            [0x03] = "END",
            [0x05] = "DELAY",
            [0x06] = "WAIT",
            [0x07] = "SYNC",
            [0x08] = "CHOICE",
            [0x09] = "END_CHOICE",
            [0x0E] = "VAR",
            [0x10] = "IF",
            [0x11] = "IF_NOT",
            [0x12] = "LNAME",
            [0x13] = "FNAME",
            [0x14] = "NNAME",
            [0x1F] = "DBL_TAB",
            [0x20] = "SPACE",
            [0x21] = "HALF_TAB",
        };

        private static readonly IReadOnlyDictionary<byte, string> ExtendedCodes = new Dictionary<byte, string>
        {
            [0x31] = "COLOR",
            [0x32] = "SYM",
            [0x33] = "ITEM_WITH_TYPE",
        };

        private static readonly IReadOnlyDictionary<byte, string> ColorNames = new Dictionary<byte, string>
        {
            [0x01] = "WHITE_1",
            [0x05] = "WHITE_5",
            [0x07] = "LIGHT_BLUE",
            [0x0B] = "LIME",
            [0x0D] = "ORANGE",
            [0x14] = "YELLOW",
            [0x15] = "TEAL",
            [0x16] = "FUCHSIA",
            [0x17] = "LIGHT_GRAY",
            [0x18] = "GREEN",
            [0x19] = "BLACK",
            [0x10] = "BLUE",
            [0x11] = "PINK",
            [0x22] = "NAME_GREEN",
            [0x23] = "RANK",
            [0x13] = "LIME_2",
            [0x11] = "WHITE_1_ALT",
            [0x12] = "WHITE",
            [0x02] = "DEFAULT",
        };

        private static readonly IReadOnlyDictionary<byte, string> SymbolNames = new Dictionary<byte, string>
        {
            [0x01] = "UNK_01",
            [0x02] = "UNK_02",
            [0x03] = "UNK_03",
            [0x04] = "UNK_04",
            [0x05] = "UNK_05",
            [0x06] = "UNK_06",
            [0x07] = "HEART",
            [0x08] = "UNK_08",
            [0x09] = "UNK_09",
            [0x0A] = "UNK_0A",
            [0x0B] = "UNK_0B",
            [0x0C] = "UNK_0C",
            [0x0D] = "UNK_0D",
            [0x0E] = "UNK_0E",
            [0x0F] = "UNK_0F",
            [0x10] = "UNK_10",
            [0x11] = "UNK_11",
            [0x12] = "UNK_12",
            [0x13] = "LQUOTE",
            [0x14] = "RQUOTE",
            [0x15] = "UNK_15",
        };

        private static readonly IReadOnlyDictionary<byte, string> SpecialChars = new Dictionary<byte, string>
        {
            [0x01] = "\n",
            [0x20] = " ",
        };
        #endregion

        public EBD Convert(BinaryFormat source)
        {
            if (source is null)
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
            ebd.AdditionalData["StartToStringData"] = reader.ReadBytes(blockLength);

            reader.Stream.Position = originalPosition;
        }

        private static void ParseEntries(DataReader reader, EBD ebd)
        {
            List<TextPointer> pointers = ExtractTextPointers(reader, ebd);

            uint id = 0;
            foreach (TextPointer p in pointers)
            {
                reader.Stream.Position = p.TextAddress;
                string text = ReadTextUntilEntryEnd(reader);

                ebd.Entries.Add(new EBD.Entry
                {
                    Id = id++,
                    Offset = p.PointerAddress,
                    Text = text,
                });
            }
        }

        private static List<TextPointer> ExtractTextPointers(DataReader reader, EBD ebd)
        {
            var pointers = new List<TextPointer>();
            reader.Stream.Position = ebd.Header.InstructionsPos;

            long endPosition = ebd.Header.ArgumentsPos;

            while (reader.Stream.Position < endPosition)
            {
                int instruction = reader.ReadInt32();
                int argument = reader.ReadInt32();

                if (instruction != InstructionTextReference)
                    continue;

                long pointerAddress = argument;

                long returnPos = reader.Stream.Position;

                reader.Stream.Position = pointerAddress;
                int textOffset = reader.ReadInt32();
                long textAddress = ebd.Header.StringDataPos + textOffset;

                pointers.Add(new TextPointer(pointerAddress, textAddress));

                reader.Stream.Position = returnPos;
            }

            return pointers;
        }

        private static string ReadTextUntilEntryEnd(DataReader reader)
        {
            var sb = new StringBuilder();

            while (TryReadUInt16(reader, out ushort word))
            {
                if (word == EntryEnd)
                    break;

                byte upper = (byte)(word >> 8);
                byte lower = (byte)(word & 0xFF);

                AppendToken(reader, sb, upper, lower, word);
            }

            return sb.ToString();
        }

        private static void AppendToken(DataReader reader, StringBuilder sb, byte upper, byte lower, ushort raw)
        {
            if (upper == PrefixExtended)
            {
                TryAppendExtended(reader, sb, lower);
                return;
            }

            if (upper == PrefixControlWithArg)
            {
                TryAppendControlWithArg(reader, sb, lower);
                return;
            }

            if (upper == PrefixSimpleControl)
            {
                AppendSimpleControl(sb, lower);
                return;
            }

            if (CharMap.Value.TryGetValue(raw, out string mapped))
            {
                sb.Append(mapped);
                return;
            }

            sb.Append((char)raw);
        }

        // 0x14XX: [opcode][0x0000][0x0000][value]
        private static bool TryAppendExtended(DataReader reader, StringBuilder sb, byte opcode)
        {
            if (!EnsureAvailable(reader, 6))
                return false;

            _ = reader.ReadUInt16(); // 0
            _ = reader.ReadUInt16(); // 0
            ushort val = reader.ReadUInt16();

            if (!ExtendedCodes.TryGetValue(opcode, out string codeName))
            {
                AppendUnknownExtended(sb, opcode, val);
                return true;
            }

            if (codeName == "COLOR")
            {
                AppendColor(sb, val);
                return true;
            }

            if (codeName == "SYM")
            {
                AppendSymbol(sb, val);
                return true;
            }

            if (codeName == "ITEM_WITH_TYPE")
            {
                AppendTagWithValue(sb, "ITEM_WITH_TYPE", val);
                return true;
            }

            AppendTagWithValue(sb, codeName, val);
            return true;
        }

        private static void AppendUnknownExtended(StringBuilder sb, byte opcode, ushort val)
        {
            sb.Append("{UNK14");
            sb.Append(opcode.ToString("X2", CultureInfo.InvariantCulture));
            sb.Append(":");
            sb.Append(val.ToString(CultureInfo.InvariantCulture));
            sb.Append("}");
        }

        private static void AppendColor(StringBuilder sb, ushort val)
        {
            byte colorValue = (byte)(val & 0xFF);
            string name = ColorNames.TryGetValue(colorValue, out string cn)
                ? cn
                : "UNK_" + colorValue.ToString("X2", CultureInfo.InvariantCulture);

            sb.Append("{COLOR:");
            sb.Append(name);
            sb.Append("}");
        }

        private static void AppendSymbol(StringBuilder sb, ushort val)
        {
            byte symValue = (byte)(val & 0xFF);

            // Some files store swapped already.
            byte swapped = SwapNibbles(symValue);
            byte lookup = SymbolNames.ContainsKey(swapped) ? swapped : symValue;

            string symName = SymbolNames.TryGetValue(lookup, out string sn)
                ? sn
                : "UNK_" + lookup.ToString("X2", CultureInfo.InvariantCulture);

            if (symName == "LQUOTE" || symName == "RQUOTE")
            {
                sb.Append('"');
                return;
            }

            sb.Append("{SYM:");
            sb.Append(symName);
            sb.Append("}");
        }

        private static void AppendTagWithValue(StringBuilder sb, string tag, ushort val)
        {
            sb.Append("{");
            sb.Append(tag);
            sb.Append(":");
            sb.Append(val.ToString(CultureInfo.InvariantCulture));
            sb.Append("}");
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
                sb.Append(arg.ToString(CultureInfo.InvariantCulture));
                sb.Append("}");
                return true;
            }

            sb.Append("{UNK");
            sb.Append(opcode.ToString("X2", CultureInfo.InvariantCulture));
            sb.Append(":");
            sb.Append(arg.ToString(CultureInfo.InvariantCulture));
            sb.Append("}");
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
            sb.Append(opcode.ToString("X2", CultureInfo.InvariantCulture));
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

        private static bool EnsureAvailable(DataReader reader, int bytes) =>
            reader.Stream.Position + bytes <= reader.Stream.Length;

        private static byte SwapNibbles(byte value) =>
            (byte)(((value & 0x0F) << 4) | ((value & 0xF0) >> 4));

        private readonly struct TextPointer
        {
            public TextPointer(long pointerAddress, long textAddress)
            {
                PointerAddress = pointerAddress;
                TextAddress = textAddress;
            }

            public long PointerAddress { get; }
            public long TextAddress { get; }
        }

        private static IReadOnlyDictionary<ushort, string> LoadCharMapNextToExe()
        {
            string baseDir = AppContext.BaseDirectory;
            string path = Path.Combine(baseDir, CharMapFileName);

            // Si no existe, no rompemos: simplemente no mapeamos nada.
            if (!File.Exists(path))
                return new Dictionary<ushort, string>();

            var map = new Dictionary<ushort, string>();

            foreach (string rawLine in File.ReadLines(path, Encoding.UTF8))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                    continue;

                int comma = line.IndexOf(',');
                if (comma < 0)
                    throw new FormatException($"Línea inválida (sin coma) en {CharMapFileName}: {rawLine}");

                string codeText = line.Substring(0, comma).Trim();
                string valueText = line.Substring(comma + 1).Trim();

                if (valueText.Length == 0)
                    throw new FormatException($"Línea inválida (valor vacío) en {CharMapFileName}: {rawLine}");

                ushort code = ParseUShortHexOrDec(codeText);

                // Permite 1 o más chars, por si luego quieres tokens especiales
                map[code] = valueText;
            }

            return map;
        }

        private static ushort ParseUShortHexOrDec(string s)
        {
            s = s.Trim();

            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                return ushort.Parse(s.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

            return ushort.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }
    }
}
