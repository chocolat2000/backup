using System;
using System.Collections.Generic;
using Cassandra.Mapping.Attributes;
using Newtonsoft.Json;

namespace BackupDatabase.Models
{
    [Table(Name = "users")]
    public class DBUser
    {
        [JsonProperty("login")]
        [Column("login")]
        [PartitionKey]
        public string Login { get; set; }

        [JsonProperty("password")]
        [Column("password")]
        public string Password { get; set; }

        [JsonProperty("roles")]
        [Column("roles")]
        public IEnumerable<string> Roles { get; set; }
    }
}
