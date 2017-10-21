using System;
using Cassandra.Mapping;
using Cassandra.Mapping.Attributes;

namespace BackupDatabase.Models
{
    [Table(Name = "blocks")]
    public class DBBlock
    {
        [Column("id")]
        [Ignore]
        public Guid Id { get; set; }
        [Column("murmur")]
        [PartitionKey]
        public Guid Murmur { get; set; }
        [Column("data")]
        public byte[] Data { get; set; }
    }

}
