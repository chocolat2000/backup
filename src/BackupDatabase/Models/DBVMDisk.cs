using System;
using Cassandra.Mapping;
using Cassandra.Mapping.Attributes;
using Newtonsoft.Json;

namespace BackupDatabase.Models
{
    [Table(Name = "vmdisks")]
    public class DBVMDisk
    {
        [JsonProperty("id")]
        [Column("id")]
        public Guid Id { get; set; }

        [JsonProperty("key")]
        [Column("key")]
        [ClusteringKey]
        public int Key { get; set; }

        [JsonProperty("path")]
        [Column("path")]
        public string Path { get; set; }

        [JsonProperty("changeid")]
        [Column("changeid")]
        public string ChangeId { get; set; }

        [JsonProperty("metadata")]
        [Column("metadata")]
        public byte[] Metadata { get; set; }

        [JsonProperty("vm")]
        [Column("vm")]
        [PartitionKey]
        public Guid VM { get; set; }

        [JsonProperty("length")]
        [Column("length")]
        public long Length { get; set; }

        [JsonProperty("valid")]
        [Column("valid")]
        public bool Valid { get; set; }

    }
}
