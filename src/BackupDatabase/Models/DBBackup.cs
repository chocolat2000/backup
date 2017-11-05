using System;
using System.Collections.Generic;
using Cassandra.Mapping.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BackupDatabase.Models
{
    [Table(Name = "dbbackup")]
    public class DBBackup
    {
        [JsonProperty("id")]
        [Column("id")]
        [SecondaryIndex]
        public Guid Id { get; set; }

        [JsonProperty("server")]
        [Column("server")]
        [PartitionKey]
        public Guid Server { get; set; }

        [JsonProperty("startdate")]
        [Column("startdate")]
        [ClusteringKey]
        public DateTime StartDate { get; set; }

        [JsonProperty("enddate")]
        [Column("enddate")]
        public DateTime EndDate { get; set; }

        [JsonProperty("status")]
        [JsonConverter(typeof(StringEnumConverter))]
        [Column("status", Type = typeof (string))]
        public Status Status { get; set; }

        [JsonProperty("log")]
        [Column("log")]
        public IEnumerable<string> Log { get; set; }

        public void AppendLog(string line)
        {
            if (Log is List<string>)
            {
                ((List<string>)Log).Add(line);
            }
            else
            {
                Log = new List<string>(Log)
                {
                    line
                };
            }
        }

    }

    public enum Status
    {
        Running,
        Failed,
        Warning,
        Successful,
        Cancelled
    }
}
