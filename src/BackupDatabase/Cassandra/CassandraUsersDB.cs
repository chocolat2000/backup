using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BackupDatabase.Models;
using Cassandra;
using Cassandra.Data.Linq;
using Crypto;

namespace BackupDatabase.Cassandra
{
    public class CassandraUsersDB : IUsersDBAccess
    {

        private Cluster casCluster;

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

        private Table<DBUser> TblUsers => new Table<DBUser>(Conn);

        public CassandraUsersDB(params string[] addresses)
        {
            casCluster = Cluster.Builder().AddContactPoints(addresses).Build();
            TblUsers.CreateIfNotExists();
        }

        public async Task<DBUser> GetUser(string login, string password)
        {
            var user = await TblUsers.Where(u => u.Login == login).FirstOrDefault().ExecuteAsync().ConfigureAwait(false);
            if (user == null)
                return null;

            using (var hashing = new Hashing())
            {
                return await hashing.VerifyPassword(password, user.Password).ConfigureAwait(false) ? user : null;
            }
        }

        public async Task AddUser(string login, string password, IEnumerable<string> roles = null)
        {
            using (var hashing = new Hashing())
            {
                var p = await hashing.HashPassword(password).ConfigureAwait(false);
                await TblUsers.Insert(new DBUser { Login = login, Password = p, Roles = roles }).ExecuteAsync().ConfigureAwait(false);
            }
        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (conn != null)
                    {
                        conn.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~CassandraMetaDB() {
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
