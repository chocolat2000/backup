using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BackupDatabase.Models;
using Cassandra;
using Cassandra.Data.Linq;
using Cassandra.Mapping;

namespace BackupDatabase.Cassandra
{
    public class CassandraMetaDB : IMetaDBAccess
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

        private Table<DBWindowsServer> TblWindowsServer => new Table<DBWindowsServer>(Conn);
        private Table<DBVMwareServer> TblVMwareServer => new Table<DBVMwareServer>(Conn);
        private Table<DBServer> TblServers => new Table<DBServer>(Conn);
        private Table<DBBackup> TblBackups => new Table<DBBackup>(Conn);
        private Table<DBFile> TblFiles => new Table<DBFile>(Conn);
        private Table<DBFolder> TblFolder => new Table<DBFolder>(Conn);
        private Table<DBFileBlock> TblFileBlocks => new Table<DBFileBlock>(Conn);
        private Table<DBVMwareVM> TblVMwareVM => new Table<DBVMwareVM>(Conn);
        private Table<DBVMDisk> TblVMDisks => new Table<DBVMDisk>(Conn);
        private Table<DBVMDiskBlock> TblVMDiskBlocks => new Table<DBVMDiskBlock>(Conn);
        private Table<DBCalendarEntry> TblCalendarEntry => new Table<DBCalendarEntry>(Conn);


        public CassandraMetaDB(params string[] addresses)
        {
            casCluster = Cluster.Builder().AddContactPoints(addresses).Build();
            //TblWindowsServer.CreateIfNotExists();
            TblVMwareServer.CreateIfNotExists();
            TblBackups.CreateIfNotExists();
            TblFiles.CreateIfNotExists();
            TblFolder.CreateIfNotExists();
            TblFileBlocks.CreateIfNotExists();
            TblVMwareVM.CreateIfNotExists();
            TblVMDisks.CreateIfNotExists();
            TblVMDiskBlocks.CreateIfNotExists();
            TblCalendarEntry.CreateIfNotExists();
        }


        public async Task<Guid> AddBackup(DBBackup backup)
        {
            if (backup.Id == Guid.Empty)
            {
                backup.Id = Guid.NewGuid();

            }
            await TblBackups.Insert(backup, false).ExecuteAsync().ConfigureAwait(false);
            return backup.Id;
        }

        public async Task<Guid> AddCalendarEntry(DBCalendarEntry entry)
        {
            if (entry.Id == Guid.Empty)
            {
                entry.Id = Guid.NewGuid();
            }
            await TblCalendarEntry.Insert(entry, false).ExecuteAsync().ConfigureAwait(false);
            return entry.Id;
        }

        public async Task<Guid> AddFile(DBFile file)
        {
            if (file.Id == Guid.Empty)
            {
                file.Id = Guid.NewGuid();
            }
            await TblFiles.Insert(file, false).ExecuteAsync().ConfigureAwait(false);
            return file.Id;
        }

        public async Task AddFileBlock(DBFileBlock fileBlock)
        {
            fileBlock.Id = Guid.NewGuid();
            await TblFileBlocks.Insert(fileBlock, false).ExecuteAsync().ConfigureAwait(false);
        }

        public async Task AddFileBlocks(IEnumerable<DBFileBlock> fileBlocks)
        {
            await Task.Run(() => Parallel.ForEach(fileBlocks, fileBlock =>
            {
                fileBlock.Id = Guid.NewGuid();
                TblFileBlocks.Insert(fileBlock, false).Execute();
            })).ConfigureAwait(false);
        }

        public async Task<Guid> AddFolder(DBFolder folder)
        {
            if (folder.Id == Guid.Empty)
            {
                folder.Id = Guid.NewGuid();
            }
            await TblFolder.Insert(folder, false).ExecuteAsync().ConfigureAwait(false);
            return folder.Id;
        }

        public async Task<Guid> AddServer(DBServer server)
        {
            if (server.Id == Guid.Empty)
            {
                server.Id = Guid.NewGuid();
            }
            if (server is DBVMwareServer)
            {
                await TblVMwareServer.Insert((DBVMwareServer)server, false).ExecuteAsync().ConfigureAwait(false);
            }
            else if (server is DBWindowsServer)
            {
                await TblWindowsServer.Insert((DBWindowsServer)server, false).ExecuteAsync().ConfigureAwait(false);
            }

            return server.Id;
        }

        public async Task<Guid> AddVMDisk(DBVMDisk disk)
        {
            if (disk.Id == Guid.Empty)
            {
                disk.Id = Guid.NewGuid();
            }
            await TblVMDisks.Insert(disk, false).ExecuteAsync().ConfigureAwait(false);
            return disk.Id;
        }

        public async Task AddVMDiskBlock(DBVMDiskBlock vmDiskBlock)
        {
            if (vmDiskBlock.Id == Guid.Empty)
            {
                vmDiskBlock.Id = Guid.NewGuid();
            }
            await TblVMDiskBlocks.Insert(vmDiskBlock, false).ExecuteAsync().ConfigureAwait(false);
        }

        public Task AddVMDiskBlocks(IEnumerable<DBVMDiskBlock> vmDiskBlocks)
        {
            throw new NotImplementedException();
        }

        public async Task<Guid> AddVMwareVM(DBVMwareVM vm)
        {
            if (vm.Id == Guid.Empty)
            {
                vm.Id = Guid.NewGuid();
            }
            await TblVMwareVM.Insert(vm).ExecuteAsync().ConfigureAwait(false);
            return vm.Id;
        }

        public async Task<long> BackupSize(Guid backupId)
        {
            var files = await TblFiles.Where(f => f.Backup == backupId).Select(f => f.Length).ExecuteAsync().ConfigureAwait(false);
            return files.Sum();
        }

        public async Task CopyVMDiskBlocks(Guid sourceDiskId, Guid destinationDiskId)
        {
            var blocks = await TblVMDiskBlocks.Where(b => b.VMDisk == sourceDiskId).ExecuteAsync().ConfigureAwait(false);
            await Task.Run(() => Parallel.ForEach(blocks, block =>
            {
                block.VMDisk = destinationDiskId;
                TblVMDiskBlocks.Insert(block).Execute();
            })).ConfigureAwait(false);
        }

        public async Task<DBBackup> GetBackup(Guid backupId)
        {
            return await TblBackups.Where(backup => backup.Id == backupId).FirstOrDefault().ExecuteAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<DBBackup>> GetBackups()
        {
            return await TblBackups.Take(20).ExecuteAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<DBBackup>> GetBackups(DateTime from)
        {
            return await TblBackups.Where(backup => backup.StartDate >= from).Take(20).ExecuteAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<DBBackup>> GetBackups(DateTime from, DateTime to)
        {
            return await TblBackups.Where(backup => backup.StartDate >= from && backup.StartDate <= to).Take(20).ExecuteAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<DBBackup>> GetBackups(Guid server)
        {
            return await TblBackups.Where(backup => backup.Server == server).Take(20).ExecuteAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<DBVMwareVM>> GetVMwareVM(Guid backup)
        {
            return await TblVMwareVM.Where(vm => vm.Backup == backup).ExecuteAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<DBVMwareVM>> GetServerVMwareVM(Guid server)
        {
            return await TblVMwareVM.Where(vm => vm.Server == server).ExecuteAsync().ConfigureAwait(false);
        }

        public async Task<DBCalendarEntry> GetCalendarEntry(Guid entry)
        {
            return await TblCalendarEntry.Where(c => c.Id == entry).FirstOrDefault().ExecuteAsync().ConfigureAwait(false);
        }

        public async Task<DBFile> GetFile(Guid file)
        {
            return await TblFiles.Where(f => f.Id == file).FirstOrDefault().ExecuteAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<DBFileBlock>> GetFileBlocks(Guid fileId)
        {
            return await TblFileBlocks.Where(b => b.File == fileId).ExecuteAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<DBFile>> GetFiles(Guid backup)
        {
            return await TblFiles.Where(f => f.Backup == backup).ExecuteAsync().ConfigureAwait(false);
        }

        public Task<IEnumerable<DBFile>> GetFiles(Guid backup, string folder)
        {
            throw new NotImplementedException();
        }

        public Task<DBFolder> GetFolder(Guid folder)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<DBFolder>> GetFolders(Guid backup, string pre = "")
        {
            throw new NotImplementedException();
        }

        public async Task<DBVMwareVM> GetLatestVM(Guid server, string vmMoref)
        {
            return await TblVMwareVM.Where(vm => vm.Server == server && vm.Moref == vmMoref).FirstOrDefault().ExecuteAsync().ConfigureAwait(false);
        }

        public async Task<long> GetNextVMDiskOffset(Guid diskId, long offset)
        {
            return await TblVMDiskBlocks.Where(b => b.VMDisk == diskId && b.Offset > offset).Select(b => b.Offset).FirstOrDefault().ExecuteAsync().ConfigureAwait(false);
        }

        public async Task<long> GetPreviousVMDiskOffset(Guid diskId, long offset)
        {
            return await TblVMDiskBlocks.Where(b => b.VMDisk == diskId && b.Offset < offset).OrderByDescending(b => b.Offset).Select(b => b.Offset).FirstOrDefault().ExecuteAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<DBCalendarEntry>> GetServerCalendar(Guid server)
        {
            return await TblCalendarEntry.Where(c => c.Server == server).ExecuteAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<DBCalendarEntry>> GetNextEntries()
        {
            //return await TblCalendarEntry.Where(e => e.NextRun <= DateTime.UtcNow).AllowFiltering().ExecuteAsync().ConfigureAwait(false);
            var mapper = new Mapper(conn);
            return await mapper.FetchAsync<DBCalendarEntry>("SELECT * from calendar WHERE enabled=true AND nextrun <= ? ALLOW FILTERING", DateTime.UtcNow);
        }

        public async Task<ServerType> GetServerType(Guid id)
        {
            return await TblWindowsServer.Where(s => s.Id == id).Select(s => s.Type).FirstOrDefault().ExecuteAsync().ConfigureAwait(false);
        }

        public async Task<DBVMDisk> GetVMDisk(Guid vm, int key)
        {
            return await TblVMDisks.Where(d => d.VM == vm && d.Key == key).FirstOrDefault().ExecuteAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<DBVMDiskBlock>> GetVMDiskBlocks(Guid vmDiskId)
        {
            return await TblVMDiskBlocks.Where(b => b.VMDisk == vmDiskId).ExecuteAsync().ConfigureAwait(false);
        }

        public async Task<DBVMwareServer> GetVMWareServer(Guid id)
        {
            return await TblVMwareServer
                .Where(s => s.Id == id)
                // Don't extract credentials from DB !
                .Select(server => new DBVMwareServer { Id = server.Id, Name = server.Name, Ip = server.Ip, Port = server.Port, Type = server.Type, VMs = server.VMs })
                .FirstOrDefault().ExecuteAsync().ConfigureAwait(false);
        }

        public DBVMwareServer GetVMWareServerSync(Guid id)
        {
            return TblVMwareServer
                .Where(s => s.Id == id)
                // Don't extract credentials from DB !
                .Select(server => new DBVMwareServer { Id = server.Id, Name = server.Name, Ip = server.Ip, Port = server.Port, Type = server.Type, VMs = server.VMs })
                .FirstOrDefault().Execute();
        }

        public async Task<DBWindowsServer> GetWindowsServer(Guid id)
        {
            return await TblWindowsServer
                .Where(s => s.Id == id)
                // Don't extract credentials from DB !
                .Select(server => new DBWindowsServer { Id = server.Id, Name = server.Name, Ip = server.Ip, Port = server.Port, Type = server.Type })
                .FirstOrDefault().ExecuteAsync().ConfigureAwait(false);
        }

        public DBWindowsServer GetWindowsServerSync(Guid id)
        {
            return TblWindowsServer
                .Where(s => s.Id == id)
                // Don't extract credentials from DB !
                .Select(server => new DBWindowsServer { Id = server.Id, Name = server.Name, Ip = server.Ip, Port = server.Port, Type = server.Type })
                .FirstOrDefault().Execute();
        }

        public async Task<IEnumerable<DBServer>> GetServers()
        {
            return await TblServers
                // Don't extract credentials from DB !
                .Select(server => new DBServer { Id = server.Id, Name = server.Name, Ip = server.Ip, Port = server.Port, Type = server.Type })
                .ExecuteAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<DBWindowsServer>> GetWindowsServers()
        {
            return await TblWindowsServer
                .Where(s => s.Type == ServerType.Windows)
                // Don't extract credentials from DB !
                .Select(server => new DBWindowsServer { Id = server.Id, Name = server.Name, Ip = server.Ip, Port = server.Port, Type = server.Type })
                .ExecuteAsync().ConfigureAwait(false);
        }

        public async Task<IEnumerable<DBVMwareServer>> GetVMwareServers()
        {
            return await TblVMwareServer
                .Where(s => s.Type == ServerType.VMware)
                // Don't extract credentials from DB !
                .Select(server => new DBVMwareServer { Id = server.Id, Name = server.Name, Ip = server.Ip, Port = server.Port, Type = server.Type, VMs = server.VMs })
                .ExecuteAsync().ConfigureAwait(false);
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
