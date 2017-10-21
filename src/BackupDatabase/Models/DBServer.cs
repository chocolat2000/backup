using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using Cassandra.Mapping.Attributes;

namespace BackupDatabase.Models
{
    [Table("servers")]
    public class DBServer
    {
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [Column("id")]
        [PartitionKey]
        public Guid Id { get; set; }

        [JsonProperty("name")]
        [Column("name")]
        [SecondaryIndex]
        public string Name { get; set; }

        [JsonProperty("ip")]
        [Column("ip")]
        public string Ip { get; set; }

        [JsonProperty("port", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [Column("port")]
        public int Port { get; set; }

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        [Column("type", Type = typeof (string))]
        [SecondaryIndex]
        public ServerType Type { get; set; }
    }

    public enum ServerType
    {
        Undefined,
        Windows,
        Linux,
        VMware,
        VM
    }

}

