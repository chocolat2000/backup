using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cassandra.Mapping;
using Cassandra.Mapping.Attributes;


namespace BackupDatabase.Models
{
    [Table(Name = "files")]
    public class DBFile
    {
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
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
