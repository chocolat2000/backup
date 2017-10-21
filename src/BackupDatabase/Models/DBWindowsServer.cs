using Newtonsoft.Json;
using Cassandra.Mapping.Attributes;

namespace BackupDatabase.Models
{
    public class DBWindowsServer : DBServer
    {
        [JsonProperty("username")]
        [Column("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        [Column("password")]
        public string Password { get; set; }

    }

}
