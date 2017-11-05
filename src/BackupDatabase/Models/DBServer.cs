using System;
using Cassandra.Mapping.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BackupDatabase.Models
{
    [Table("servers")]
    public class DBServer
    {
        [JsonProperty("id")]
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

        [JsonProperty("port")]
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

