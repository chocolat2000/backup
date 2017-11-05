using System;
using Cassandra.Mapping;
using Cassandra.Mapping.Attributes;
using Newtonsoft.Json;

namespace BackupDatabase.Models
{
    [Table(Name = "vmdisk_blocks")]
    public class DBVMDiskBlock
    {
        [JsonProperty("vmdisk")]
        [Column("vmdisk")]
        [PartitionKey]
        public Guid VMDisk { get; set; }

        [JsonProperty("block")]
        [Column("block")]
        public Guid Block { get; set; }

        [JsonProperty("offset")]
        [Column("offset")]
        [ClusteringKey]
        public long Offset { get; set; }

    }
}
