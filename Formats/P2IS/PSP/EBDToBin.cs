using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Yarhl.FileFormat;
using Yarhl.IO;
using Yarhl.Media.Text.Encodings;
using ClassicPersonaToolkit.Helpers.P2IS.PSP.Models;

namespace ClassicPersonaToolkit.Formats.P2IS.PSP
{
    public sealed class EbdToBin : IConverter<EBD, BinaryFormat>
    {
        private const ushort EntryEnd = 0x1103;
        private const ushort NewLineCode = 0x1101;

        private const ushort SimpleControlBase = 0x1100;
        private const ushort ControlWithArgBase = 0x1200;
        private const ushort ExtendedBase = 0x1400;

        private const string CharMapFileName = "char_map.csv";

        private static readonly Lazy<IReadOnlyDictionary<string, ushort>> ReverseCharMap =
            new Lazy<IReadOnlyDictionary<string, ushort>>(LoadReverseCharMapNextToExe, isThreadSafe: true);

        // Credits to eiowlta for the control codes
        #region Control Codes
        private static readonly IReadOnlyDictionary<string, byte> ControlCodes = new Dictionary<string, byte>(StringComparer.Ordinal)
        {
            ["NL"] = 0x01,
            ["CLEAR"] = 0x02,
            ["END"] = 0x03,
            ["DELAY"] = 0x05,
            ["WAIT"] = 0x06,
            ["SYNC"] = 0x07,
            ["CHOICE"] = 0x08,
            ["END_CHOICE"] = 0x09,
            ["VAR"] = 0x0E,
            ["IF"] = 0x10,
            ["IF_NOT"] = 0x11,
            ["LNAME"] = 0x12,
            ["FNAME"] = 0x13,
            ["NNAME"] = 0x14,
            ["DBL_TAB"] = 0x1F,
            ["SPACE"] = 0x20,
            ["HALF_TAB"] = 0x21,
        };

        private static readonly IReadOnlyDictionary<string, byte> ExtendedCodes = new Dictionary<string, byte>(StringComparer.Ordinal)
        {
            ["COLOR"] = 0x31,
            ["SYM"] = 0x32,
            ["ITEM_WITH_TYPE"] = 0x33,
        };

        private static readonly IReadOnlyDictionary<string, byte> ColorNames = new Dictionary<string, byte>(StringComparer.Ordinal)
        {
            ["WHITE_1"] = 0x01,
            ["WHITE_5"] = 0x05,
            ["LIGHT_BLUE"] = 0x07,
            ["LIME"] = 0x0B,
            ["ORANGE"] = 0x0D,
            ["YELLOW"] = 0x14,
            ["TEAL"] = 0x15,
            ["FUCHSIA"] = 0x16,
            ["LIGHT_GRAY"] = 0x17,
            ["GREEN"] = 0x18,
            ["BLACK"] = 0x19,
            ["BLUE"] = 0x10,
            ["PINK"] = 0x11,
            ["NAME_GREEN"] = 0x22,
            ["RANK"] = 0x23,
            ["LIME_2"] = 0x13,
            ["WHITE_1_ALT"] = 0x11,
            ["WHITE"] = 0x12,
            ["DEFAULT"] = 0x02,
        };

        private static readonly IReadOnlyDictionary<string, byte> SymbolNames = new Dictionary<string, byte>(StringComparer.Ordinal)
        {
            ["UNK_01"] = 0x01,
            ["UNK_02"] = 0x02,
            ["UNK_03"] = 0x03,
            ["UNK_04"] = 0x04,
            ["UNK_05"] = 0x05,
            ["UNK_06"] = 0x06,
            ["HEART"] = 0x07,
            ["UNK_08"] = 0x08,
            ["UNK_09"] = 0x09,
            ["UNK_0A"] = 0x0A,
            ["UNK_0B"] = 0x0B,
            ["UNK_0C"] = 0x0C,
            ["UNK_0D"] = 0x0D,
            ["UNK_0E"] = 0x0E,
            ["UNK_0F"] = 0x0F,
            ["UNK_10"] = 0x10,
            ["UNK_11"] = 0x11,
            ["UNK_12"] = 0x12,
            ["LQUOTE"] = 0x13,
            ["RQUOTE"] = 0x14,
            ["UNK_15"] = 0x15,
        };
        #endregion

        public BinaryFormat Convert(EBD source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var output = new BinaryFormat();
            var writer = new DataWriter(output.Stream)
            {
                DefaultEncoding = new EscapeOutRangeEncoding(Encoding.Unicode),
            };

            WriteHeader(writer, source);
            WriteAdditionalDataAndUpdatePointers(writer, source);
            WriteEntries(writer, source);

            return output;
        }

        private static void WriteHeader(DataWriter writer, EBD ebd)
        {
            writer.Write(ebd.Header.StartPos);
            writer.Write(ebd.Header.FunctionsPos);
            writer.Write(ebd.Header.NumberOfFunctions);
            writer.Write(ebd.Header.InstructionsPos);
            writer.Write(ebd.Header.ArgumentsPos);
            writer.Write(ebd.Header.StringDataPos);
        }

        private static void WriteAdditionalDataAndUpdatePointers(DataWriter writer, EBD ebd)
        {
            if (!TryGetAdditionalBlock(ebd, "StartToStringData", out byte[] block))
                return;

            Dictionary<int, int> textOffsets = CalculateTextOffsets(ebd);

            long blockStart = writer.Stream.Position;
            writer.Write(block);
            long blockEnd = writer.Stream.Position;

            foreach (var entry in ebd.Entries)
            {
                long pointerPosition = entry.Offset;

                if (pointerPosition < blockStart || pointerPosition >= blockEnd)
                    continue;

                long returnPos = writer.Stream.Position;
                writer.Stream.Position = pointerPosition;

                int newOffset = textOffsets[(int)entry.Id];
                writer.Write(newOffset);

                writer.Stream.Position = returnPos;
            }
        }

        private static bool TryGetAdditionalBlock(EBD ebd, string key, out byte[] bytes)
        {
            bytes = Array.Empty<byte>();
            return ebd.AdditionalData.TryGetValue(key, out var data) && data is byte[] b && (bytes = b) != null;
        }

        private static Dictionary<int, int> CalculateTextOffsets(EBD ebd)
        {
            var offsets = new Dictionary<int, int>(capacity: ebd.Entries.Count);
            int currentOffset = 0;

            foreach (var entry in ebd.Entries.OrderBy(e => e.Id))
            {
                offsets[(int)entry.Id] = currentOffset;

                int textSize = CalculateCompiledTextSize(entry.Text) + sizeof(ushort); // EntryEnd
                currentOffset += textSize;
            }

            return offsets;
        }

        private static int CalculateCompiledTextSize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;

            int size = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];

                if (ch == '{' && TryReadTag(text, i, out _, out int nextIndex))
                {
                    string tag = text.Substring(i + 1, nextIndex - i - 2);
                    size += CalculateTagSize(tag);
                    i = nextIndex - 1;
                    continue;
                }

                if (ch == '\n')
                {
                    size += sizeof(ushort);
                    continue;
                }

                if (ch == '"')
                {
                    size += 4 * sizeof(ushort); // extended
                    continue;
                }

                if (ch == ' ')
                {
                    size += sizeof(ushort); // SPACE control
                    continue;
                }

                if (ReverseCharMap.Value.ContainsKey(ch.ToString()))
                {
                    size += sizeof(ushort);
                    continue;
                }

                size += sizeof(ushort); // regular UTF-16 char
            }

            return size;
        }

        private static int CalculateTagSize(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return 0;

            string command = GetCommand(tag);

            if (IsKnownExtendedCommand(command) || command.StartsWith("UNK14", StringComparison.Ordinal))
                return 4 * sizeof(ushort);

            if (HasArgument(tag))
                return 2 * sizeof(ushort);

            return sizeof(ushort);
        }

        private static bool IsKnownExtendedCommand(string command) =>
            command.Equals("COLOR", StringComparison.Ordinal) ||
            command.Equals("SYM", StringComparison.Ordinal) ||
            command.Equals("ITEM_WITH_TYPE", StringComparison.Ordinal);

        private static bool HasArgument(string tag) => tag.IndexOf(':') >= 0;

        private static string GetCommand(string tag)
        {
            int colon = tag.IndexOf(':');
            return colon >= 0 ? tag.Substring(0, colon) : tag;
        }

        private static void WriteEntries(DataWriter writer, EBD ebd)
        {
            foreach (var entry in ebd.Entries.OrderBy(e => e.Id))
            {
                WriteText(writer, entry.Text);
                writer.Write(EntryEnd);
            }
        }

        private static void WriteText(DataWriter writer, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var reverseMap = ReverseCharMap.Value;

            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];

                if (ch == '{' && TryReadTag(text, i, out string tag, out int nextIndex))
                {
                    WriteTag(writer, tag);
                    i = nextIndex - 1;
                    continue;
                }

                if (ch == '\n')
                {
                    writer.Write(NewLineCode);
                    continue;
                }

                if (ch == '"')
                {
                    bool isLeft = i == 0 || text[i - 1] == ' ' || text[i - 1] == '\n';
                    byte symValue = isLeft ? SwapNibbles(0x13) : SwapNibbles(0x14);
                    WriteExtended(writer, ExtendedCodes["SYM"], symValue);
                    continue;
                }

                if (ch == ' ')
                {
                    WriteSimpleControl(writer, ControlCodes["SPACE"]);
                    continue;
                }

                if (reverseMap.TryGetValue(ch.ToString(), out ushort gameCode))
                {
                    writer.Write(gameCode);
                    continue;
                }

                writer.Write((ushort)ch);
            }
        }

        private static bool TryReadTag(string text, int openBraceIndex, out string tag, out int nextIndex)
        {
            tag = string.Empty;
            nextIndex = openBraceIndex;

            int close = text.IndexOf('}', openBraceIndex);
            if (close < 0)
                return false;

            tag = text.Substring(openBraceIndex + 1, close - openBraceIndex - 1);
            nextIndex = close + 1;
            return true;
        }

        private static void WriteTag(DataWriter writer, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return;

            SplitTag(tag, out string command, out string? arg);

            if (TryWriteKnownExtended(writer, command, arg))
                return;

            if (TryWriteUnknownExtended(writer, command, arg))
                return;

            if (TryWriteKnownControl(writer, command, arg))
                return;

            TryWriteUnknownControl(writer, command, arg);
        }

        private static void SplitTag(string tag, out string command, out string? arg)
        {
            int colon = tag.IndexOf(':');
            if (colon < 0)
            {
                command = tag;
                arg = null;
                return;
            }

            command = tag.Substring(0, colon);
            arg = colon + 1 < tag.Length ? tag.Substring(colon + 1) : string.Empty;
        }

        private static bool TryWriteKnownExtended(DataWriter writer, string command, string? arg)
        {
            if (arg is null)
                return false;

            if (command.Equals("COLOR", StringComparison.Ordinal))
            {
                if (TryResolveNamedOrUnknownByte(ColorNames, arg, out byte value))
                    WriteExtended(writer, ExtendedCodes["COLOR"], value);
                return true;
            }

            if (command.Equals("SYM", StringComparison.Ordinal))
            {
                if (TryResolveNamedOrUnknownByte(SymbolNames, arg, out byte value))
                    WriteExtended(writer, ExtendedCodes["SYM"], SwapNibbles(value));
                return true;
            }

            if (command.Equals("ITEM_WITH_TYPE", StringComparison.Ordinal) &&
                ushort.TryParse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort itemValue))
            {
                WriteExtended(writer, ExtendedCodes["ITEM_WITH_TYPE"], itemValue);
                return true;
            }

            return false;
        }

        private static bool TryWriteUnknownExtended(DataWriter writer, string command, string? arg)
        {
            if (!command.StartsWith("UNK14", StringComparison.Ordinal) || arg is null)
                return false;

            if (!ushort.TryParse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort value))
                return true;

            if (command.Length >= 7 && TryParseHexByte(command.AsSpan(5, 2), out byte opcode))
                WriteExtended(writer, opcode, value);

            return true;
        }

        private static bool TryWriteKnownControl(DataWriter writer, string command, string? arg)
        {
            if (!ControlCodes.TryGetValue(command, out byte opcode))
                return false;

            if (arg is not null && ushort.TryParse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort argValue))
            {
                WriteControlWithArg(writer, opcode, argValue);
                return true;
            }

            WriteSimpleControl(writer, opcode);
            return true;
        }

        private static void TryWriteUnknownControl(DataWriter writer, string command, string? arg)
        {
            if (!command.StartsWith("UNK", StringComparison.Ordinal) || command.Length < 5)
                return;

            if (!TryParseHexByte(command.AsSpan(3, 2), out byte opcode))
                return;

            if (arg is not null && ushort.TryParse(arg, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort argValue))
            {
                WriteControlWithArg(writer, opcode, argValue);
            }
            else
            {
                WriteSimpleControl(writer, opcode);
            }
        }

        private static bool TryResolveNamedOrUnknownByte(IReadOnlyDictionary<string, byte> namedMap, string token, out byte value)
        {
            if (namedMap.TryGetValue(token, out value))
                return true;

            if (token.StartsWith("UNK_", StringComparison.Ordinal) && token.Length >= 6)
                return TryParseHexByte(token.AsSpan(4, 2), out value);

            value = 0;
            return false;
        }

        private static bool TryParseHexByte(ReadOnlySpan<char> hex2, out byte value) =>
            byte.TryParse(hex2, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);

        private static void WriteExtended(DataWriter writer, byte opcode, ushort value)
        {
            writer.Write((ushort)(ExtendedBase | opcode));
            writer.Write((ushort)0x0000);
            writer.Write((ushort)0x0000);
            writer.Write(value);
        }

        private static void WriteExtended(DataWriter writer, byte opcode, byte value) =>
            WriteExtended(writer, opcode, (ushort)value);

        private static void WriteControlWithArg(DataWriter writer, byte opcode, ushort arg)
        {
            writer.Write((ushort)(ControlWithArgBase | opcode));
            writer.Write(arg);
        }

        private static void WriteSimpleControl(DataWriter writer, byte opcode) =>
            writer.Write((ushort)(SimpleControlBase | opcode));

        private static byte SwapNibbles(byte value) =>
            (byte)(((value & 0x0F) << 4) | ((value & 0xF0) >> 4));

        private static IReadOnlyDictionary<string, ushort> LoadReverseCharMapNextToExe()
        {
            string baseDir = AppContext.BaseDirectory;
            string path = Path.Combine(baseDir, CharMapFileName);

            if (!File.Exists(path))
                return new Dictionary<string, ushort>(StringComparer.Ordinal);

            var map = new Dictionary<string, ushort>(StringComparer.Ordinal);

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

                map[valueText] = code;
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
