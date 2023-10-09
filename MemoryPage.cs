using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Actividad9
{
    public class MemoryPage : IMemoryItem
    {
        public int PageID { get; set; }
        public int Size { get; set; }
        public int ProcessID { get; set; } // -1 if not allocated to any process

        public MemoryPage(int pageID, int size)
        {
            PageID = pageID;
            Size = size;
            ProcessID = -1; // Initialize as unallocated
        }
    }
}
