using System;
using Newtonsoft.Json;
using Cassandra.Mapping;
using Cassandra.Mapping.Attributes;

namespace BackupDatabase.Models
{
    [Table(Name = "vmdisk_blocks")]
    public class DBVMDiskBlock
    {
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [Column("id")]
        [Ignore]
        public Guid Id { get; set; }

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
