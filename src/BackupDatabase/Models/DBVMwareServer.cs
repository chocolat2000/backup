using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Cassandra.Mapping;
using Cassandra.Mapping.Attributes;

namespace BackupDatabase.Models
{
    public class DBVMwareServer : DBServer
    {
        [JsonProperty("thumbPrint")]
        [Column("thumbPrint")]
        public string ThumbPrint { get; set; }

        [JsonProperty("username")]
        [Column("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        [Column("password")]
        public string Password { get; set; }

        [JsonProperty("vms")]
        [Column("vms", Type = typeof (Dictionary<string, string>))]
        public IDictionary<string, string> VMs { get; set; }
    }

}
