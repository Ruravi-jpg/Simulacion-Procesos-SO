using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Actividad9
{
    public class MemoryBlock : IMemoryItem
    {
        public int BlockID { get; set; }
        public int Size { get; set; }
        public int ProcessID { get; set; }

        public MemoryBlock(int blockID, int size)
        {
            BlockID = blockID;
            Size = size;
            ProcessID = -1;
        }
    }
}
