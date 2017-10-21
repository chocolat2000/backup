using BackupDatabase.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BackupDatabase
{
    public interface IDBHashes : IDisposable
    {
        Task AddHash(Guid hash, Guid block);

        Task<IEnumerable<Guid>> GetBlocksFromHash(Guid hash);

    }
}
