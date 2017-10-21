using Newtonsoft.Json;
using System;
using Cassandra.Mapping;
using Cassandra.Mapping.Attributes;


namespace BackupDatabase.Models
{
    [Table(Name = "file_blocks")]
    public class DBFileBlock
    {
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [Column("id")]
        [PartitionKey]
        public Guid Id { get; set; }

        [JsonProperty("file")]
        [Column("file")]
        [SecondaryIndex]
        public Guid File { get; set; }

        [JsonProperty("block")]
        [Column("block")]
        public Guid Block { get; set; }

        [JsonProperty("offset")]
        [Column("offset")]
        public long Offset { get; set; }

    }
}
