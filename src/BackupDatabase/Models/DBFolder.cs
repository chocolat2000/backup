using Newtonsoft.Json;
using System;
using Cassandra.Mapping;
using Cassandra.Mapping.Attributes;

namespace BackupDatabase.Models
{
    [Table(Name = "folders")]
    public class DBFolder
    {
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [Ignore]
        public Guid Id { get; set; }

        [JsonProperty("name")]
        [Column("name")]
        [ClusteringKey]
        public string Name { get; set; }

        [JsonProperty("backup")]
        [Column("backup")]
        [PartitionKey]
        public Guid Backup { get; set; }

        [JsonProperty("date")]
        [Column("date")]
        public DateTime LastWriteTime { get; set; }

    }
}
