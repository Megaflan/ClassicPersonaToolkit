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
            Header = new HeaderInfo();
            AdditionalData = new Dictionary<string, byte[]>();
        }

        public class Entry
        {
            public uint Id { get; set; }
            public long Offset { get; set; }
            public required string Text { get; set; }
        }

        public class HeaderInfo
        {
            public uint StartPos { get; set; }
            public uint FunctionsPos { get; set; }
            public uint NumberOfFunctions { get; set; }
            public uint InstructionsPos { get; set; }
            public uint ArgumentsPos { get; set; }
            public uint StringDataPos { get; set; }
        }

        public Collection<Entry> Entries { get; }
        public HeaderInfo? Header { get; set; }
        public Dictionary<string, byte[]> AdditionalData { get; set; }
        public bool HasCompleteOriginalData => Header != null;
    }
}
