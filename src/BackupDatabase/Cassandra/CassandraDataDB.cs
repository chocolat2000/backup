using BackupDatabase.Models;
using Cassandra;
using Cassandra.Data.Linq;
using BackupDatabase.Cassandra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackupDatabase.Cassandra
{
    public class CassandraDataDB : IDataDBAccess
    {

        private Cluster casCluster;

        private Dictionary<string, Table<DBBlock>> blocksTable;

        private ISession conn;
        private ISession Conn
        {
            get
            {
                if (conn == null || conn.IsDisposed)
                {
                    conn = casCluster.Connect();
                    conn.CreateKeyspaceIfNotExists("backup");
                    conn.ChangeKeyspace("backup");
                }
                return conn;
            }
        }

        public CassandraDataDB(params string[] addresses)
        {
            casCluster = Cluster.Builder().AddContactPoints(addresses).Build();
            var hexa = new char[] { 'a', 'b', 'c', 'd', 'e', 'f', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            blocksTable = new Dictionary<string, Table<DBBlock>>(hexa.Length * hexa.Length);
            foreach (var a in hexa)
            {
                foreach(var b in hexa)
                {
                    var tb = new Table<DBBlock>(Conn, null, $"blocks_data_{a}{b}");
                    tb.CreateIfNotExists();
                    blocksTable[$"{a}{b}"] = tb;
                }
            }
        }

        public Task<byte[]> ReadBlock(Guid id)
        {
            var guidArray = id.ToString();

            return blocksTable[$"{guidArray[0]}{guidArray[1]}"]
                .Where((b) => b.Murmur == id).Select((b) => b.Data)
                .FirstOrDefault()
                .ExecuteAsync();
        }

        public Task WriteBlock(Guid id, byte[] data)
        {
            return WriteBlock(id, data, data.Length);
        }

        public Task WriteBlock(Guid id, byte[] data, int length)
        {
            var guidArray = id.ToString();
            var block = new DBBlock
            {
                Murmur = id
            };

            if (length < data.Length)
            {
                block.Data = new byte[length];
                Array.Copy(data, block.Data, length);
            }
            else
            {
                block.Data = data;
            }

            return blocksTable[$"{guidArray[0]}{guidArray[1]}"]
                .Insert(block)
                .ExecuteAsync();
        }

    }
}
