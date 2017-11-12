using System;
using Cassandra.Mapping.Attributes;
using Newtonsoft.Json;

namespace BackupDatabase.Models
{
    [Table(Name = "files_blocks")]
    public class DBFileBlock
    {
        [JsonProperty("id")]
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
