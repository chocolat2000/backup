using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Cassandra.Mapping;
using Cassandra.Mapping.Attributes;

namespace BackupDatabase.Models
{
    [Table(Name = "hashes")]
    public class DBHash
    {
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [Column("id")]
        [PartitionKey]
        public Guid Id { get; set; }

        [JsonProperty("blocks")]
        [Column("blocks", Type = typeof (List<Guid>))]
        public IEnumerable<Guid> Blocks { get; set; }
    }
}
