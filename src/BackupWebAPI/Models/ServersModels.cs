using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vim25Proxy.Models;

namespace BackupWebAPI.Models
{
    public class VMwareArbo
    {
        public IEnumerable<ManagedEntity> Folders { get; set; }
        public IEnumerable<ManagedEntity> Pools { get; set; }

    }
}
