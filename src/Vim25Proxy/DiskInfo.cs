using System;
using System.Collections.Generic;
using System.Text;

namespace Vim25Proxy
{
    public class DiskInfo
    {
        public string Path { get; set; }
        public string ChangeId { get; set; }
        public int Key { get; set; }
        public long Capacity { get; set; }
    }
}
