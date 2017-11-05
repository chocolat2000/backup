using System.Collections.Generic;
using Cassandra.Mapping.Attributes;
using Newtonsoft.Json;

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
