using Cassandra;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Cassandra.Data.Linq;
using BackupDatabase.Models;

namespace BackupDatabase.Cassandra
{
    public class CassandraHashesDB : IDBHashes
    {
        private Cluster CasCluster;
        private PreparedStatement IncStatement => Session.Prepare($"UPDATE {TblDBHash.Name } SET references = references + 1 WHERE hash = ? AND block = ?");
        private PreparedStatement DecStatement => Session.Prepare($"UPDATE {TblDBHash.Name } SET references = references - 1 WHERE hash = ? AND block = ?");

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
                }
                return session;
            }
        }

        private Table<DBHash> tblDBHash;
        private Table<DBHash> TblDBHash
        {
            get
            {
                if (tblDBHash == null)
                {
                    tblDBHash = new Table<DBHash>(Session);
                    tblDBHash.CreateIfNotExists();
                }
                return tblDBHash;
            }
        }

        /*
        private Table<DBBlockReferences> tblDBBlockReferences;
        private Table<DBBlockReferences> TblDBBlockReferences
        {
            get
            {
                if (tblDBBlockReferences == null)
                {
                    tblDBBlockReferences = new Table<DBBlockReferences>(Session);
                    tblDBBlockReferences.CreateIfNotExists();
                }
                return tblDBBlockReferences;
            }
        }
        */


        public CassandraHashesDB(params string[] addresses)
        {
            CasCluster = Cluster.Builder().AddContactPoints(addresses).Build();
        }

        /*
        public async Task IncReference(Guid block, Guid hash)
        {
            //await TblDBBlockReferences.Where(r => r.Block == block && r.Hash == hash).Select(r => new DBBlockReferences { References = r.References + 1 }).Update().ExecuteAsync().ConfigureAwait(false);
            await Session.ExecuteAsync(IncStatement.Bind(block, hash)).ConfigureAwait(false);
        }

        public async Task DecReference(Guid block, Guid hash)
        {
            //await TblDBBlockReferences.Where(r => r.Block == block && r.Hash == hash).Select(r => new DBBlockReferences { References = r.References - 1 }).Update().ExecuteAsync().ConfigureAwait(false);
            await Session.ExecuteAsync(DecStatement.Bind(block, hash)).ConfigureAwait(false);
        }
        */

        public async Task AddHash(Guid hash, Guid block)
        {
            //await TblDBHash.Insert(new DBHash { Hash = hash, Block = block }).ExecuteAsync().ConfigureAwait(false);
            await Session.ExecuteAsync(IncStatement.Bind(hash, block)).ConfigureAwait(false);
        }

        public async Task<IEnumerable<Guid>> GetBlocksFromHash(Guid hash)
        {
            return await TblDBHash.Where(dbhash => dbhash.Hash == hash).Select(dbhash => dbhash.Block).ExecuteAsync().ConfigureAwait(false);
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
