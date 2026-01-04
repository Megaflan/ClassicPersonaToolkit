using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Yarhl.FileFormat;
using Yarhl.IO;
using Yarhl.Media.Text.Encodings;
using ClassicPersonaToolkit.Helpers.P2IS.PSP.Models;

namespace ClassicPersonaToolkit.Formats.P2IS.PSP
{
    public class EbdToBin : IConverter<EBD, BinaryFormat>
    {
        private const ushort EntryMarker = 0x1431;
        private const ushort EntryEnd = 0x1103;
        private const int MarkerSkipBytes = 0x4;

        private static readonly IReadOnlyDictionary<string, byte> ControlCodes = new Dictionary<string, byte>
        {
            { "NL", 0x01 },
            { "CLEAR", 0x02 },
            { "END", 0x03 },
            { "DELAY", 0x05 },
            { "WAIT", 0x06 },
            { "SYNC", 0x07 },
            { "CHOICE", 0x08 },
            { "END_CHOICE", 0x09 },
            { "VAR", 0x0E },
            { "IF", 0x10 },
            { "IF_NOT", 0x11 },
            { "LNAME", 0x12 },
            { "FNAME", 0x13 },
            { "NNAME", 0x14 },
            { "DBL_TAB", 0x1F },
            { "SPACE", 0x20 },
            { "HALF_TAB", 0x21 },
        };

        private static readonly IReadOnlyDictionary<string, byte> ExtendedCodes = new Dictionary<string, byte>
        {
            { "COLOR", 0x31 },
            { "SYM", 0x32 },
            { "ITEM_WITH_TYPE", 0x33 },
        };

        private static readonly IReadOnlyDictionary<string, byte> ColorNames = new Dictionary<string, byte>
        {
            { "WHITE_1", 0x01 },
            { "WHITE_5", 0x05 },
            { "LIGHT_BLUE", 0x07 },
            { "LIME", 0x0B },
            { "ORANGE", 0x0D },
            { "YELLOW", 0x14 },
            { "TEAL", 0x15 },
            { "FUCHSIA", 0x16 },
            { "LIGHT_GRAY", 0x17 },
            { "GREEN", 0x18 },
            { "BLACK", 0x19 },
            { "BLUE", 0x20 },
            { "PINK", 0x21 },
            { "NAME_GREEN", 0x32 },
            { "RANK", 0x33 },
            { "LIME_2", 0x13 },
            { "WHITE_1_ALT", 0x11 },
            { "WHITE", 0x12 },
            { "DEFAULT", 0x02 },
        };

        private static readonly IReadOnlyDictionary<string, byte> SymbolNames = new Dictionary<string, byte>
        {
            { "UNK_01", 0x01 },
            { "UNK_02", 0x02 },
            { "UNK_03", 0x03 },
            { "UNK_04", 0x04 },
            { "UNK_05", 0x05 },
            { "UNK_06", 0x06 },
            { "HEART", 0x07 },
            { "UNK_08", 0x08 },
            { "UNK_09", 0x09 },
            { "UNK_0A", 0x0A },
            { "UNK_0B", 0x0B },
            { "UNK_0C", 0x0C },
            { "UNK_0D", 0x0D },
            { "UNK_0E", 0x0E },
            { "UNK_0F", 0x0F },
            { "UNK_10", 0x10 },
            { "UNK_11", 0x11 },
            { "UNK_12", 0x12 },
            { "LQUOTE", 0x13 },
            { "RQUOTE", 0x14 },
            { "UNK_15", 0x15 },
        };

        public BinaryFormat Convert(EBD source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var output = new BinaryFormat();
            var writer = new DataWriter(output.Stream)
            {
                DefaultEncoding = new EscapeOutRangeEncoding(Encoding.Unicode),
            };

            WriteHeader(writer, source);
            WriteAdditionalData(writer, source);
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

        private static void WriteAdditionalData(DataWriter writer, EBD ebd)
        {
            if (ebd.AdditionalData.TryGetValue("StartToStringData", out var data) && data is byte[] bytes)
            {
                writer.Write(bytes);
            }
        }

        private static void WriteEntries(DataWriter writer, EBD ebd)
        {
            foreach (var entry in ebd.Entries)
            {
                writer.Write(EntryMarker);

                // Write marker skip bytes (4 bytes of padding/data)
                for (int i = 0; i < MarkerSkipBytes; i++)
                {
                    writer.Write((byte)0x00);
                }

                writer.Write((short)0x0022);

                WriteText(writer, entry.Text);
                writer.Write(EntryEnd);
            }
        }

        private static void WriteText(DataWriter writer, string text)
        {
            int i = 0;
            while (i < text.Length)
            {
                if (text[i] == '{')
                {
                    int closeIndex = text.IndexOf('}', i);
                    if (closeIndex != -1)
                    {
                        string tag = text.Substring(i + 1, closeIndex - i - 1);
                        WriteTag(writer, tag);
                        i = closeIndex + 1;
                        continue;
                    }
                }

                if (text[i] == '\n')
                {
                    writer.Write((ushort)0x1101);
                    i++;
                    continue;
                }

                if (text[i] == '"')
                {
                    // Check if this is a left or right quote based on context
                    bool isLeft = i == 0 || text[i - 1] == ' ' || text[i - 1] == '\n';
                    byte symValue = isLeft ? SwapNibbles((byte)0x13) : SwapNibbles((byte)0x14);
                    WriteExtendedCode(writer, 0x32, symValue);
                    i++;
                    continue;
                }

                if (text[i] == ' ')
                {
                    WriteSimpleControl(writer, ControlCodes["SPACE"]);
                    i++;
                    continue;
                }

                // Write regular character
                writer.Write((ushort)text[i]);
                i++;
            }
        }

        private static void WriteTag(DataWriter writer, string tag)
        {
            string[] parts = tag.Split(':');
            string command = parts[0];
            string arg = parts.Length > 1 ? parts[1] : null;

            // Check for extended codes
            if (command == "COLOR" && arg != null)
            {
                if (ColorNames.TryGetValue(arg, out byte colorValue))
                {
                    WriteExtendedCode(writer, ExtendedCodes["COLOR"], colorValue);
                }
                else if (arg.StartsWith("UNK_") && byte.TryParse(arg.Substring(4), System.Globalization.NumberStyles.HexNumber, null, out byte unknownColor))
                {
                    WriteExtendedCode(writer, ExtendedCodes["COLOR"], unknownColor);
                }
                return;
            }

            if (command == "SYM" && arg != null)
            {
                if (SymbolNames.TryGetValue(arg, out byte symValue))
                {
                    byte swapped = SwapNibbles(symValue);
                    WriteExtendedCode(writer, ExtendedCodes["SYM"], swapped);
                }
                else if (arg.StartsWith("UNK_") && byte.TryParse(arg.Substring(4), System.Globalization.NumberStyles.HexNumber, null, out byte unknownSym))
                {
                    WriteExtendedCode(writer, ExtendedCodes["SYM"], unknownSym);
                }
                return;
            }

            if (command == "ITEM_WITH_TYPE" && arg != null && ushort.TryParse(arg, out ushort itemValue))
            {
                WriteExtendedCode(writer, ExtendedCodes["ITEM_WITH_TYPE"], itemValue);
                return;
            }

            // Check for unknown extended codes (UNK14XX)
            if (command.StartsWith("UNK14") && arg != null && ushort.TryParse(arg, out ushort unknownExtValue))
            {
                if (byte.TryParse(command.Substring(5), System.Globalization.NumberStyles.HexNumber, null, out byte unknownExtOpcode))
                {
                    WriteExtendedCode(writer, unknownExtOpcode, unknownExtValue);
                }
                return;
            }

            // Check for control codes with arguments
            if (ControlCodes.TryGetValue(command, out byte opcode) && arg != null && ushort.TryParse(arg, out ushort argValue))
            {
                WriteControlWithArg(writer, opcode, argValue);
                return;
            }

            // Check for simple control codes
            if (ControlCodes.TryGetValue(command, out byte simpleOpcode))
            {
                WriteSimpleControl(writer, simpleOpcode);
                return;
            }

            // Check for unknown opcodes
            if (command.StartsWith("UNK") && command.Length > 3)
            {
                string opcodeHex = command.Substring(3, 2);
                if (byte.TryParse(opcodeHex, System.Globalization.NumberStyles.HexNumber, null, out byte unknownOpcode))
                {
                    if (arg != null && ushort.TryParse(arg, out ushort unknownArgValue))
                    {
                        WriteControlWithArg(writer, unknownOpcode, unknownArgValue);
                    }
                    else
                    {
                        WriteSimpleControl(writer, unknownOpcode);
                    }
                }
            }
        }

        private static void WriteExtendedCode(DataWriter writer, byte opcode, ushort value)
        {
            writer.Write((ushort)0x1400 | opcode);
            writer.Write((ushort)0x0000);
            writer.Write((ushort)0x0000);
            writer.Write(value);
        }

        private static void WriteExtendedCode(DataWriter writer, byte opcode, byte value)
        {
            WriteExtendedCode(writer, opcode, (ushort)value);
        }

        private static void WriteControlWithArg(DataWriter writer, byte opcode, ushort arg)
        {
            writer.Write((ushort)(0x1200 | opcode));
            writer.Write(arg);
        }

        private static void WriteSimpleControl(DataWriter writer, byte opcode)
        {
            writer.Write((ushort)(0x1100 | opcode));
        }

        private static byte SwapNibbles(byte value)
        {
            return (byte)(((value & 0x0F) << 4) | ((value & 0xF0) >> 4));
        }
    }
}