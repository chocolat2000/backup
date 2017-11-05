using System;
using Cassandra.Mapping.Attributes;
using Newtonsoft.Json;

namespace BackupDatabase.Models
{
    [Table(Name = "folders")]
    public class DBFolder
    {
        [JsonProperty("id")]
        [Column("id")]
        [PartitionKey]
        public Guid Id { get; set; }

        [JsonProperty("name")]
        [Column("name")]
        [ClusteringKey]
        public string Name { get; set; }

        [JsonProperty("backup")]
        [Column("backup")]
        public Guid Backup { get; set; }

        [JsonProperty("date")]
        [Column("date")]
        public DateTime LastWriteTime { get; set; }

    }
}
