using System;
using Cassandra.Mapping.Attributes;
using Newtonsoft.Json;

namespace BackupDatabase.Models
{
    [Table(Name = "block_references")]
    public class DBBlockReferences
    {
        [JsonProperty("block")]
        [Column("block")]
        [PartitionKey]
        public Guid Block { get; set; }

        [JsonProperty("hash")]
        [Column("hash")]
        [ClusteringKey]
        public Guid Hash { get; set; }

        [JsonProperty("references")]
        [Column("references")]
        [Counter]
        public long References { get; set; }

    }
}
