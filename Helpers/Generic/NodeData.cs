using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassicPersonaToolkit.Helpers.Generic
{
    public class NodeData
    {
        public int Id { get; set; }
        public long Position { get; set; }
        public long Size { get; set; }

        public NodeData(int id, long position, long size)
        {
            Id = id;
            Position = position;
            Size = size;
        }
    }
}
