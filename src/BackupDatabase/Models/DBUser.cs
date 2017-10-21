using Cassandra.Mapping.Attributes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace BackupDatabase.Models
{
    [Table(Name = "dbuser")]
    public class DBUser
    {
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [Ignore]
        public Guid Id { get; set; }

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
