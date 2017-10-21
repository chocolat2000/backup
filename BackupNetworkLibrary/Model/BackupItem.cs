using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BackupNetworkLibrary.Model
{
    [DataContract]
    public class BackupItem
    {

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public BackupItemType Type { get; set; }

        [DataMember]
        public DateTime LastWriteTime { get; set; }

        [DataMember]
        public long Length { get; set; }

        [DataMember]
        public Guid StreamGuid { get; set; }

    }

    public enum BackupItemType
    {
        File,
        Folder
    }
}
