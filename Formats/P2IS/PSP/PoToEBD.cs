using ClassicPersonaToolkit.Helpers.P2IS.PSP.Models;
using Yarhl.FileFormat;
using Yarhl.Media.Text;
using System;
using System.Linq;

namespace ClassicPersonaToolkit.Formats.P2IS.PSP
{
    public class PoToEBD : IConverter<Po, EBD>
    {
        public EBD Convert(Po source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            EBD ebd = new EBD();

            if (source.Header?.Extensions != null &&
                source.Header.Extensions.ContainsKey("X-EBD-Start"))
            {
                ebd.Header = new EBD.HeaderInfo
                {
                    StartPos = ParseHexValue(source.Header.Extensions["X-EBD-Start"]),
                    FunctionsPos = ParseHexValue(source.Header.Extensions["X-EBD-Functions"]),
                    NumberOfFunctions = ParseHexValue(source.Header.Extensions["X-EBD-NumberOfFunctions"]),
                    InstructionsPos = ParseHexValue(source.Header.Extensions["X-EBD-Instructions"]),
                    ArgumentsPos = ParseHexValue(source.Header.Extensions["X-EBD-Arguments"]),
                    StringDataPos = ParseHexValue(source.Header.Extensions["X-EBD-StringData"])
                };
                Console.WriteLine("> Header restored from X-EBD-* fields");
                Console.WriteLine(ebd.Header.ToString());
            }
            else
            {
                Console.WriteLine("<!> Warning: PO file does not contain header information");
            }

            if (source.Header?.Extensions != null)
            {
                var dataKeys = source.Header.Extensions.Keys
                    .Where(k => k.StartsWith("X-EBD-") && k.EndsWith("-Data"))
                    .ToList();

                foreach (var key in dataKeys)
                {
                    string sectionName = key.Substring(6, key.Length - 11); // Extract name between "X-EBD-" and "-Data"
                    string base64 = source.Header.Extensions[key];
                    byte[] data = System.Convert.FromBase64String(base64);
                    ebd.AdditionalData[sectionName] = data;
                    Console.WriteLine($"> {sectionName}: {data.Length} bytes restored from Base64");
                }
            }

            foreach (var entry in source.Entries)
            {
                var contextParts = entry.Context.Split(',');
                var idPart = contextParts[0].Split(':')[1];
                var offsetPart = contextParts[1].Split(':')[1];

                uint id = uint.Parse(idPart);
                long offset = System.Convert.ToInt64(offsetPart, offsetPart.StartsWith("0x") ? 16 : 10);

                string text = string.IsNullOrEmpty(entry.Translated) ? entry.Original : entry.Translated;

                if (text == "<empty>")
                    text = "";

                ebd.Entries.Add(new EBD.Entry
                {
                    Id = id,
                    Offset = offset,
                    Text = text
                });
            }

            Console.WriteLine($"\n> {ebd.Entries.Count} entries restored from PO");

            return ebd;
        }

        private uint ParseHexValue(string hexString)
        {
            if (hexString.StartsWith("0x") || hexString.StartsWith("0X"))
                hexString = hexString.Substring(2);

            return (uint)System.Convert.ToInt32(hexString, 16);
        }
    }
}