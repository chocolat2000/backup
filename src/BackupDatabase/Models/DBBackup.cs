using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using Cassandra.Mapping;
using Cassandra.Mapping.Attributes;

namespace BackupDatabase.Models
{
    [Table(Name = "dbbackup")]
    public class DBBackup
    {
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
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
            var log = Log.ToList();
            log.Add(line);
            Log = log;
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
