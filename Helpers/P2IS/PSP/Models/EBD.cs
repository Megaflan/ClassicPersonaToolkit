using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yarhl.FileFormat;

namespace ClassicPersonaToolkit.Helpers.P2IS.PSP.Models
{
    public class EBD : IFormat
    {
        public EBD()
        {
            Entries = new Collection<Entry>();
        }

        public class Entry
        {
            public uint Id { get; set; }
            public long Offset { get; set; }
            public required string Text { get; set; }
        }

        public Collection<Entry> Entries { get; }
    }
}
