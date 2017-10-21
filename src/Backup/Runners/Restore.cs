using Backup.Services;
using BackupDatabase;
using BackupDatabase.Cassandra;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Backup.Runners
{
    public class Restore
    {
        private IMetaDBAccess dataBase;
        private CassandraDataDB cassandraDB;

        public Restore(IMetaDBAccess dataBase)
        {
            this.dataBase = dataBase;
            cassandraDB = new CassandraDataDB();
        }

        public async Task RestoreFile(Guid file, string destination)
        {
            var _file = await dataBase.GetFile(file);
            var fileBlocks = await dataBase.GetFileBlocks(file);
            var targetName = Path.Combine(destination, Path.GetFileName(_file.Name));
            using (var fileToWrite = File.Open(targetName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fileToWrite.SetLength(_file.Length);

                foreach (var fileBlock in fileBlocks)
                {
                    fileToWrite.Position = fileBlock.Offset;
                    byte[] block = await cassandraDB.ReadBlock(fileBlock.Block);
                    await fileToWrite.WriteAsync(block, 0, block.Length);
                }
            }

            File.SetLastWriteTimeUtc(targetName, _file.LastWriteTime);
        }

    }
}
