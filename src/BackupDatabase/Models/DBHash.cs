using System;
using Cassandra.Mapping.Attributes;
using Newtonsoft.Json;

namespace BackupDatabase.Models
{
    [Table(Name = "hashes")]
    public class DBHash
    {
        [JsonProperty("hash")]
        [Column("hash")]
        [PartitionKey]
        public Guid Hash { get; set; }

        [JsonProperty("block")]
        [Column("block")]
        [ClusteringKey]
        public Guid Block { get; set; }

        [JsonProperty("references")]
        [Column("references")]
        [Counter]
        public long References { get; set; }
    }
}
