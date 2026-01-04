using System.Text;
using Yarhl.FileFormat;
using Yarhl.FileSystem;
using Yarhl.IO;
using Yarhl.Media.Text;
using ClassicPersonaToolkit.Helpers.P2IS.PSP.Models;

namespace ClassicPersonaToolkit.Formats.P2IS.PSP
{
    public class EBDToPo : IConverter<EBD, Po>
    {
        public Po Convert(EBD source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            Po po = new Po
            {
                Header = new PoHeader("EBD (Persona 2 IS)", "smtivesp@gmail.com", "en"),
            };

            if (source.Header != null)
            {
                Console.WriteLine(source.Header.ToString());

                po.Header.Extensions["X-EBD-Start"] = $"0x{source.Header.StartPos:X8}";
                po.Header.Extensions["X-EBD-Functions"] = $"0x{source.Header.FunctionsPos:X8}";
                po.Header.Extensions["X-EBD-NumberOfFunctions"] = $"0x{source.Header.NumberOfFunctions:X8}";
                po.Header.Extensions["X-EBD-Instructions"] = $"0x{source.Header.InstructionsPos:X8}";
                po.Header.Extensions["X-EBD-Arguments"] = $"0x{source.Header.ArgumentsPos:X8}";
                po.Header.Extensions["X-EBD-StringData"] = $"0x{source.Header.StringDataPos:X8}";

                Console.WriteLine("> Header embedded into the PO file as X-EBD-* fields");
            }
            else
            {
                Console.WriteLine("<!> Warning: EBD has no header, the PO will not contain header information");
            }

            if (source.AdditionalData.Count > 0)
            {
                foreach (var kvp in source.AdditionalData)
                {
                    string base64 = System.Convert.ToBase64String(kvp.Value);
                    po.Header.Extensions[$"X-EBD-{kvp.Key}-Data"] = base64;
                    Console.WriteLine($"> {kvp.Key}: {kvp.Value.Length} bytes → Base64 ({base64.Length} chars)");
                }

                po.Header.Extensions["X-EBD-Note"] =
                    "This PO file contains embedded binary data from the original EBD file. " +
                    "Do not remove the X-EBD-* fields as they are required for reconstruction.";
            }

            po.Header.Extensions["X-EBD-EntryCount"] = source.Entries.Count.ToString();
            po.Header.Extensions["X-EBD-ExtractedDate"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            po.Header.Extensions["X-EBD-ToolVersion"] = "ClassicPersonaToolkit v1.0";

            Console.WriteLine("\n> Additional metadata added");

            foreach (var message in source.Entries)
            {
                PoEntry entry = new PoEntry
                {
                    Context = $"id:{message.Id},offset:0x{message.Offset:X8}",
                    Original = message.Text,
                    Flags = "max-length:9999,c-format,brace-format",
                    TranslatorComment = ""
                };

                if (entry.Original == "")
                    entry.Original = "<empty>";

                po.Add(entry);
            }

            Console.WriteLine($"> {source.Entries.Count} entries added to the PO\n");

            return po;
        }
    }
}
