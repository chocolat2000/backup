using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BackupDatabase.Models;
using Cassandra;
using Cassandra.Data.Linq;
using Cassandra.Mapping;
using Crypto;

namespace BackupDatabase.Cassandra
{
    public class CassandraMetaDB : IMetaDBAccess
    {
        private Cluster casCluster;
        private Encrypt encrypt;

        private byte[] passwordsKey;
        public byte[] PasswordsKey
        {
            get { return passwordsKey; }
            set
            {
                passwordsKey = value;
                encrypt = new Encrypt(value);
            }
        }

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

        private Table<DBWindowsServer> tblWindowsServer;
        private Table<DBWindowsServer> TblWindowsServer
        {
            get
            {
                if (tblWindowsServer == null)
                {
                    tblWindowsServer = new Table<DBWindowsServer>(Conn);
                    TblVMwareServer.CreateIfNotExists();
                }
                return tblWindowsServer;
            }
        }

        private Table<DBVMwareServer> tblVMwareServer = null;
        private Table<DBVMwareServer> TblVMwareServer
        {
            get
            {
                if(tblVMwareServer == null)
                {
                    tblVMwareServer = new Table<DBVMwareServer>(Conn);
                    tblVMwareServer.CreateIfNotExists();
                }
                return tblVMwareServer;
            }
        }

        private Table<DBServer> tTblServers = null;
        private Table<DBServer> TblServers
        {
            get
            {
                if (tTblServers == null)
                {
                    tTblServers = new Table<DBServer>(Conn);
                    TblVMwareServer.CreateIfNotExists();
                }
                return tTblServers;
            }
        }

        private Table<DBBackup> tblBackups = null;
        private Table<DBBackup> TblBackups
        {
            get
            {
                if (tblBackups == null)
                {
                    tblBackups = new Table<DBBackup>(Conn);
                    tblBackups.CreateIfNotExists();
                }
                return tblBackups;
            }
        }

        private Table<DBFile> tblFiles = null;
        private Table<DBFile> TblFiles
        {
            get
            {
                if (tblFiles == null)
                {
                    tblFiles = new Table<DBFile>(Conn);
                    tblFiles.CreateIfNotExists();
                }
                return tblFiles;
            }
        }

        private Table<DBFolder> tblFolder = null;
        private Table<DBFolder> TblFolder
        {
            get
            {
                if (tblFolder == null)
                {
                    tblFolder = new Table<DBFolder>(Conn);
                    tblFolder.CreateIfNotExists();
                }
                return tblFolder;
            }
        }

        private Table<DBFileBlock> tblFileBlocks = null;
        private Table<DBFileBlock> TblFileBlocks
        {
            get
            {
                if (tblFileBlocks == null)
                {
                    tblFileBlocks = new Table<DBFileBlock>(Conn);
                    tblFileBlocks.CreateIfNotExists();
                }
                return tblFileBlocks;
            }
        }

        private Table<DBVMwareVM> tblVMwareVM = null;
        private Table<DBVMwareVM> TblVMwareVM
        {
            get
            {
                if (tblVMwareVM == null)
                {
                    tblVMwareVM = new Table<DBVMwareVM>(Conn);
                    tblVMwareVM.CreateIfNotExists();
                }
                return tblVMwareVM;
            }
        }

        private Table<DBVMDisk> tblVMDisks = null;
        private Table<DBVMDisk> TblVMDisks
        {
            get
            {
                if (tblVMDisks == null)
                {
                    tblVMDisks = new Table<DBVMDisk>(Conn);
                    tblVMDisks.CreateIfNotExists();
                }
                return tblVMDisks;
            }
        }

        private Table<DBVMDiskBlock> tblVMDiskBlocks = null;
        private Table<DBVMDiskBlock> TblVMDiskBlocks
        {
            get
            {
                if (tblVMDiskBlocks == null)
                {
                    tblVMDiskBlocks = new Table<DBVMDiskBlock>(Conn);
                    tblVMDiskBlocks.CreateIfNotExists();
                }
                return tblVMDiskBlocks;
            }
        }


        private Table<DBCalendarEntry> tblCalendarEntry = null;
        private Table<DBCalendarEntry> TblCalendarEntry
        {
            get
            {
                if (tblCalendarEntry == null)
                {
                    tblCalendarEntry = new Table<DBCalendarEntry>(Conn);
                    tblCalendarEntry.CreateIfNotExists();
                }
                return tblCalendarEntry;
            }
        }



        public CassandraMetaDB(params string[] addresses)
        {
            PasswordsKey = null;
            casCluster = Cluster.Builder().AddContactPoints(addresses).Build();
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

        public async Task<Guid> AddServer(DBServer server)
        {
            if (server.Id == Guid.Empty)
            {
                server.Id = Guid.NewGuid();
            }
            if (server is DBVMwareServer vmwareServer)
            {
                if (!string.IsNullOrWhiteSpace(vmwareServer.Password) && encrypt != null)
                {
                    vmwareServer.Password = await encrypt.Enrypt(vmwareServer.Password);
                }
                await TblVMwareServer.Insert(vmwareServer, false).ExecuteAsync().ConfigureAwait(false);
            }
            else if (server is DBWindowsServer windowsServer)
            {
                if (!string.IsNullOrWhiteSpace(windowsServer.Password) && encrypt != null)
                {
                    windowsServer.Password = await encrypt.Enrypt(windowsServer.Password);
                }
                await TblWindowsServer.Insert(windowsServer, false).ExecuteAsync().ConfigureAwait(false);
            }

            return server.Id;
        }

        public async Task DeleteServer(Guid server)
        {
            await TblWindowsServer.Where(s => s.Id == server).Delete().ExecuteAsync().ConfigureAwait(false);
        }

        public async Task<DBVMwareServer> GetVMWareServer(Guid id, bool withcreds = false)
        {
            var dbReq = TblVMwareServer.Where(s => s.Id == id);
            if(!withcreds)
            {
                // Don't extract credentials from DB !
                dbReq = dbReq.Select(s => new DBVMwareServer { Id = s.Id, Name = s.Name, Ip = s.Ip, Port = s.Port, Type = s.Type, VMs = s.VMs });
            }
            var server = await dbReq.FirstOrDefault().ExecuteAsync().ConfigureAwait(false);
            if (withcreds)
            {
                if (PasswordsKey == null)
                    throw new ArgumentNullException(nameof(PasswordsKey));

                if (string.IsNullOrWhiteSpace(server.Password))
                    throw new MissingMemberException("Password in DB is empry", nameof(server.Password));

                server.Password = await encrypt.Decrypt(server.Password);

            }

            return server;
        }

        public DBVMwareServer GetVMWareServerSync(Guid id, bool withcreds = false)
        {
            var dbReq = TblVMwareServer.Where(s => s.Id == id);
            if (!withcreds)
            {
                // Don't extract credentials from DB !
                dbReq = dbReq.Select(s => new DBVMwareServer { Id = s.Id, Name = s.Name, Ip = s.Ip, Port = s.Port, Type = s.Type, VMs = s.VMs });
            }
            var server = dbReq.FirstOrDefault().Execute();
            if (withcreds)
            {
                if (PasswordsKey == null)
                    throw new ArgumentNullException(nameof(PasswordsKey));

                if (string.IsNullOrWhiteSpace(server.Password))
                    throw new MissingMemberException("Password in DB is empry", nameof(server.Password));

                server.Password = encrypt.DecryptSync(server.Password);

            }

            return server;
        }

        public async Task<DBWindowsServer> GetWindowsServer(Guid id, bool withcreds = false)
        {
            var dbReq = TblWindowsServer.Where(s => s.Id == id);
            if (!withcreds)
            {
                // Don't extract credentials from DB !
                dbReq = dbReq.Select(s => new DBWindowsServer { Id = s.Id, Name = s.Name, Ip = s.Ip, Port = s.Port, Type = s.Type });

            }
            var server = await dbReq.FirstOrDefault().ExecuteAsync().ConfigureAwait(false);
            if (withcreds)
            {
                if (PasswordsKey == null)
                    throw new ArgumentNullException(nameof(PasswordsKey));

                if (string.IsNullOrWhiteSpace(server.Password))
                    throw new MissingMemberException("Password in DB is empry", nameof(server.Password));

                server.Password = await encrypt.Decrypt(server.Password);

            }

            return server;

        }

        public DBWindowsServer GetWindowsServerSync(Guid id, bool withcreds = false)
        {
            var dbReq = TblWindowsServer.Where(s => s.Id == id);
            if (!withcreds)
            {
                // Don't extract credentials from DB !
                dbReq = dbReq.Select(s => new DBWindowsServer { Id = s.Id, Name = s.Name, Ip = s.Ip, Port = s.Port, Type = s.Type });

            }
            var server = dbReq.FirstOrDefault().Execute();
            if (withcreds)
            {
                if (PasswordsKey == null)
                    throw new ArgumentNullException(nameof(PasswordsKey));

                if (string.IsNullOrWhiteSpace(server.Password))
                    throw new MissingMemberException("Password in DB is empry", nameof(server.Password));

                server.Password = encrypt.DecryptSync(server.Password);

            }

            return server;

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
                        if (encrypt != null)
                            encrypt.Dispose();
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
