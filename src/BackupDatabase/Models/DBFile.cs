using System;
using Cassandra.Mapping.Attributes;
using Newtonsoft.Json;

namespace BackupDatabase.Models
{
    [Table(Name = "files")]
    public class DBFile
    {
        [JsonProperty("id")]
        [Column("id")]
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

        [JsonProperty("length")]
        [Column("length")]
        public long Length { get; set; }

        [JsonProperty("valid")]
        [Column("valid")]
        public bool Valid { get; set; }

    }
}
