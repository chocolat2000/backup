using System;
using Cassandra.Mapping;
using Cassandra.Mapping.Attributes;
using Newtonsoft.Json;

namespace BackupDatabase.Models
{
    [Table("vmware_vm")]
    public class DBVMwareVM
    {
        [JsonProperty("id")]
        [Column("id")]
        public Guid Id { get; set; }

        [JsonProperty("name")]
        [Column("name")]
        public string Name { get; set; }

        [JsonProperty("moref")]
        [Column("moref")]
        [SecondaryIndex]
        public string Moref { get; set; }

        [JsonProperty("config")]
        [Column("config")]
        public byte[] Config { get; set; }

        [JsonProperty("backup")]
        [Column("backup")]
        [PartitionKey]
        public Guid Backup { get; set; }

        [JsonProperty("server")]
        [Column("server")]
        [SecondaryIndex]
        public Guid Server { get; set; }

        [JsonProperty("valid")]
        [Column("valid")]
        public bool Valid { get; set; }

        [JsonProperty("startdate")]
        [Column("startdate")]
        [ClusteringKey(0, ClusteringSortOrder = SortOrder.Descending)]
        public DateTime StartDate { get; set; }

        [JsonProperty("enddate")]
        [Column("enddate")]
        public DateTime EndDate { get; set; }

    }
}
