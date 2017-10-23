using Cassandra;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BackupDatabase.Cassandra
{
    public class CassandraHashesDB : IDBHashes
    {
        private Cluster CasCluster;
        private PreparedStatement InsertStatement => Session.Prepare("INSERT INTO hashes (hash, block) VALUES (?, ?)");
        private PreparedStatement SelectStatement => Session.Prepare("SELECT block FROM hashes WHERE hash=?");

        private ISession session = null;
        private ISession Session
        {
            get
            {
                if(session == null || session.IsDisposed)
                {
                    session = CasCluster.Connect();
                    session.CreateKeyspaceIfNotExists("backup");
                    session.ChangeKeyspace("backup");
                    session.Execute(
                        "CREATE TABLE IF NOT EXISTS hashes (" +
                        "hash uuid," +
                        "block uuid," +
                        "PRIMARY KEY (hash, block)" +
                        ")");

                }
                return session;
            }
        }

        public CassandraHashesDB(params string[] addresses)
        {
            CasCluster = Cluster.Builder().AddContactPoints(addresses).Build();
        }

        public async Task AddHash(Guid hash, Guid block)
        {
            await Session.ExecuteAsync(InsertStatement.Bind(hash, block)).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Guid>> GetBlocksFromHash(Guid hash)
        {
            var rowSet = await Session.ExecuteAsync(SelectStatement.Bind(hash)).ConfigureAwait(false);
            return rowSet.Select(row => row.GetValue<Guid>("block"));
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    session.Dispose();
                    CasCluster.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~CassandraHashesDB() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
