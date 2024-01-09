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

            foreach (var message in source.Entries)
            {
                PoEntry entry = new PoEntry
                {
                    Context = $"id:{message.Id},offset:{message.Offset}",
                    Original = message.Text,
                    Flags = "max-length:9999,c-format,brace-format",
                };

                if (entry.Original == "")
                    entry.Original = "<empty>";

                po.Add(entry);
            }

            return po;
        }
    }
}
