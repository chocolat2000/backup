using System;
using System.Collections.Generic;
using System.Text;

namespace BackupNetworkLibrary.Model
{
    public class FolderContent
    {
        public IEnumerable<string> Files { get; set; }
        public IEnumerable<string> Folders { get; set; }
    }
}
