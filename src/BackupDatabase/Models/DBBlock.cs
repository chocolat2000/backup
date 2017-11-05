using System;
using Cassandra.Mapping;
using Cassandra.Mapping.Attributes;
using Newtonsoft.Json;

namespace BackupDatabase.Models
{
    [Table(Name = "blocks")]
    public class DBBlock
    {
        [JsonProperty("murmur")]
        [Column("murmur")]
        [PartitionKey]
        public Guid Murmur { get; set; }

        [JsonProperty("data")]
        [Column("data")]
        public byte[] Data { get; set; }
    }

}
