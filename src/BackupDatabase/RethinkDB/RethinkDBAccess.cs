using BackupDatabase.Models;
using RethinkDb.Driver;
using RethinkDb.Driver.Ast;
using RethinkDb.Driver.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackupDatabase.RethinkDB
{
    public class RethinkDBAccess : IMetaDBAccess, IDBHashes
    {
        private static readonly RethinkDb.Driver.RethinkDB R = RethinkDb.Driver.RethinkDB.R;
        private static readonly Connection.Builder builder = R.Connection()
                .Hostname("127.0.0.1")
                .Port(RethinkDBConstants.DefaultPort)
                .Timeout(10);
        private static Db Database => R.Db("backup");
        private static Table TblServers => Database.Table("servers");
        private static Table TblHashes => Database.Table("hashes");
        private static Table TblBackups => Database.Table("backups");
        private static Table TblFiles => Database.Table("files");
        private static Table TblFileBlocks => Database.Table("file_blocks");
        private static Table TblVMdiskBlocks => Database.Table("vmdisk_blocks");
        private static Table TblFolders => Database.Table("folders");
        private static Table TblVMwareVM => Database.Table("vmwarevm");
        private static Table TblVMDisks => Database.Table("vmdisks");
        private static Table TblCalendar => Database.Table("calendar");

        private Connection conn;
        private Connection Conn
        {
            get
            {
                if (conn == null || !conn.Open)
                {
                    conn = builder.Connect();
                    var tables = Database.TableList().RunResult<string[]>(conn);

                    if (!tables.Contains("servers"))
                    {
                        Database.TableCreate("servers").RunResult(conn);
                        TblServers.IndexCreate("type").RunResult(conn);
                    }

                    if (!tables.Contains("hashes"))
                    {
                        Database.TableCreate("hashes").RunResult(conn);
                    }

                    if (!tables.Contains("backups"))
                    {
                        Database.TableCreate("backups").RunResult(conn);
                        TblBackups.IndexCreate("startdate").RunResult(conn);
                        TblBackups.IndexCreate("server").RunResult(conn);
                    }

                    if (!tables.Contains("files"))
                    {
                        Database.TableCreate("files").RunResult(conn);
                        TblFiles.IndexCreate("backup").RunResult(conn);
                    }

                    if (!tables.Contains("file_blocks"))
                    {
                        Database.TableCreate("file_blocks").RunResult(conn);
                        TblFileBlocks.IndexCreate("file").RunResult(conn);
                    }

                    if (!tables.Contains("vmdisk_blocks"))
                    {
                        Database.TableCreate("vmdisk_blocks").RunResult(conn);
                        TblVMdiskBlocks.IndexCreate("vmdisk").RunResult(conn);
                        TblVMdiskBlocks.IndexCreate("vmdisk_offset", row => R.Array(row.G("vmdisk"), row.G("offset"))).RunResult(conn);
                    }

                    if (!tables.Contains("folders"))
                    {
                        Database.TableCreate("folders").RunResult(conn);
                        TblFolders.IndexCreate("backup").RunResult(conn);
                    }

                    if (!tables.Contains("vmwarevm"))
                    {
                        Database.TableCreate("vmwarevm").RunResult(conn);
                        TblFileBlocks.IndexCreate("server").RunResult(conn);
                        TblFileBlocks.IndexCreate("backup").RunResult(conn);
                        TblVMwareVM.IndexCreate("backup_vm", row => R.Array(row.G("backup"), row.G("vm"))).RunResult(conn);
                    }

                    if (!tables.Contains("vmdisks"))
                    {
                        Database.TableCreate("vmdisks").RunResult(conn);
                        TblVMDisks.IndexCreate("backup_vm", row => R.Array(row.G("vm"), row.G("disk"))).RunResult(conn);
                    }

                    if (!tables.Contains("calendar"))
                    {
                        Database.TableCreate("calendar").RunResult(conn);
                        TblCalendar.IndexCreate("server").RunResult(conn);
                    }
                }
                return conn;
            }
        }

        public RethinkDBAccess()
        {
        }

        public async Task<IEnumerable<DBServer>> GetServers()
        {
            var Q = TblServers.GetAll();

            return await Q.RunCursorAsync<DBServer>(Conn);
        }

        public async Task<IEnumerable<DBWindowsServer>> GetWindowsServers()
        {
            var Q = TblServers.GetAll(ServerType.Windows.ToString()).OptArg("index", "type");

            return await Q.RunCursorAsync<DBWindowsServer>(Conn);
        }

        public async Task<IEnumerable<DBVMwareServer>> GetVMwareServers()
        {
            var Q = TblServers.GetAll(ServerType.VMware.ToString()).OptArg("index", "type");

            return await Q.RunCursorAsync<DBVMwareServer>(Conn);

        }

        public async Task DeleteServer(Guid server)
        {
            var Q = TblServers.Get(server).Delete();

            await Q.RunResultAsync(Conn);
        }

        public async Task<ServerType> GetServerType(Guid id)
        {

            var Q = TblServers.Get(id).Pluck("type");

            var typeAsString = await Q.RunAtomAsync<string>(Conn);

            ServerType serverType;
            if (Enum.TryParse(typeAsString, out serverType))
            {
                return serverType;
            }

            return ServerType.Undefined;

        }

        public async Task<DBWindowsServer> GetWindowsServer(Guid id, bool withcreds = false)
        {
            var Q = TblServers.Get(id);

            return await Q.RunAtomAsync<DBWindowsServer>(Conn);
        }

        public DBWindowsServer GetWindowsServerSync(Guid id, bool withcreds = false)
        {
            var Q = TblServers.Get(id);

            return Q.RunAtom<DBWindowsServer>(Conn);
        }

        public async Task<DBVMwareServer> GetVMWareServer(Guid id, bool withcreds = false)
        {
            var Q = TblServers.Get(id);

            return await Q.RunAtomAsync<DBVMwareServer>(Conn);
        }

        public DBVMwareServer GetVMWareServerSync(Guid id, bool withcreds = false)
        {
            var Q = TblServers.Get(id);

            return Q.RunAtom<DBVMwareServer>(Conn);
        }

        public async Task AddHash(Guid hash, Guid block)
        {
            ReqlFunction3 update = (id, old_doc, new_doc) => old_doc.Merge(new { blocks = old_doc["blocks"].Add(new_doc["blocks"]) });

            try
            {
                var Q = TblHashes.Insert(new DBHash { Id = hash, Blocks = new Guid[] { block } })
                    .OptArg("conflict", update);
                var result = await Q.RunResultAsync(Conn);

            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public async Task<IEnumerable<Guid>> GetBlocksFromHash(Guid hash)
        {
            var Q = TblHashes.Get(hash);

            return (await Q.RunAtomAsync<DBHash>(Conn)).Blocks;
        }

        public async Task<Guid> AddBackup(DBBackup backup)
        {
            try
            {
                var Q = TblBackups.Insert(backup).OptArg("conflict", "replace");
                var result = await Q.RunResultAsync(Conn);
                if (backup.Id == default(Guid))
                    return result.GeneratedKeys[0];
                else
                    return backup.Id;
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public async Task<long> BackupSize(Guid backupId)
        {
            try
            {
                var Q = TblFiles.GetAll(backupId).OptArg("index", "backup").Sum("length");
                return await Q.RunResultAsync<long>(Conn);
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        public async Task AddFileBlock(DBFileBlock fileBlock)
        {
            try
            {
                var Q = TblFileBlocks.Insert(fileBlock);
                var result = await Q.RunResultAsync(Conn);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public async Task AddFileBlocks(IEnumerable<DBFileBlock> fileBlocks)
        {
            try
            {
                var Q = TblFileBlocks.Insert(fileBlocks);
                var result = await Q.RunResultAsync(Conn);
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        public async Task<IEnumerable<DBFileBlock>> GetFileBlocks(Guid fileId)
        {
            try
            {
                var Q = TblFileBlocks.GetAll(fileId).OptArg("index", "file");
                return await Q.RunCursorAsync<DBFileBlock>(Conn);
            }
            catch (Exception e)
            {
                return Enumerable.Empty<DBFileBlock>();
            }

        }

        public async Task AddVMDiskBlock(DBVMDiskBlock vmDiskBlock)
        {
            try
            {
                var Q = TblVMdiskBlocks.Insert(vmDiskBlock);
                var result = await Q.RunResultAsync(Conn);
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public async Task AddVMDiskBlocks(IEnumerable<DBVMDiskBlock> vmDiskBlocks)
        {
            try
            {
                var Q = TblVMdiskBlocks.Insert(vmDiskBlocks);
                var result = await Q.RunResultAsync(Conn);
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        public async Task<IEnumerable<DBVMDiskBlock>> GetVMDiskBlocks(Guid vmDiskId)
        {
            try
            {
                var Q = TblVMdiskBlocks.GetAll(vmDiskId).OptArg("index", "vmdisk");
                return await Q.RunCursorAsync<DBVMDiskBlock>(Conn);
            }
            catch (Exception e)
            {
                return Enumerable.Empty<DBVMDiskBlock>();
            }

        }

        public Task CopyVMDiskBlocks(Guid sourceDiskId, Guid destinationDiskId)
        {
            var blocks = TblVMdiskBlocks
                .GetAll(sourceDiskId).OptArg("index", "vmdisk")
                .Map(block => block.Without("id").Merge(new { VMDisk = destinationDiskId }));

            return TblVMdiskBlocks
                .Insert(blocks)
                .RunResultAsync(Conn);


        }

        public async Task<long> GetPreviousVMDiskOffset(Guid diskId, long offset)
        {
            try
            {
                var blocks = TblVMdiskBlocks
                    .Between(R.Array(diskId, R.Minval()), R.Array(diskId, offset)).OptArg("index", "vmdisk_offset")
                    .Nth(-1)["offset"];

                return await blocks.RunResultAsync<long>(Conn);
            }
            catch (Exception e)
            {
                return -1;
            }

        }

        public async Task<long> GetNextVMDiskOffset(Guid diskId, long offset)
        {
            try
            {
                var blocks = TblVMdiskBlocks
                    .Between(R.Array(diskId, offset), R.Array(diskId, R.Maxval())).OptArg("index", "vmdisk_offset")
                    .Nth(0)["offset"];

                return await blocks.RunResultAsync<long>(Conn);
            }
            catch (Exception e)
            {
                return -1;
            }

        }

        public async Task<Guid> AddFile(DBFile file)
        {
            try
            {
                var Q = TblFiles.Insert(file).OptArg("conflict", "replace");
                var result = await Q.RunResultAsync(Conn);
                if (file.Id == default(Guid))
                    return result.GeneratedKeys[0];
                else
                    return file.Id;
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        public async Task<Guid> AddFolder(DBFolder folder)
        {
            try
            {
                var Q = TblFolders.Insert(folder);
                var result = await Q.RunResultAsync(Conn);
                return result.GeneratedKeys[0];
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        public async Task<Guid> AddVMwareVM(DBVMwareVM vm)
        {
            try
            {
                var Q = TblVMwareVM.Insert(vm).OptArg("conflict", "replace");
                var result = await Q.RunResultAsync(Conn);
                if (vm.Id == default(Guid))
                    return result.GeneratedKeys[0];
                else
                    return vm.Id;
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        public async Task<Guid> AddVMDisk(DBVMDisk disk)
        {
            try
            {
                var Q = TblVMDisks.Insert(disk).OptArg("conflict", "replace");
                var result = await Q.RunResultAsync(Conn);
                if (disk.Id == default(Guid))
                    return result.GeneratedKeys[0];
                else
                    return disk.Id;
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        public async Task<IEnumerable<DBBackup>> GetBackups()
        {
            try
            {
                var Q = TblBackups.OrderBy().OptArg("index", R.Desc("startdate"));
                return await Q.RunCursorAsync<DBBackup>(Conn);
            }
            catch (Exception e)
            {
                return Enumerable.Empty<DBBackup>();
            }
        }

        public async Task<IEnumerable<DBBackup>> GetBackups(DateTime from)
        {
            try
            {
                var Q = TblBackups.Filter(R.Row("startdate").Gt(from)).OrderBy().OptArg("index", R.Desc("startdate"));
                return await Q.RunCursorAsync<DBBackup>(Conn);
            }
            catch (Exception e)
            {
                return Enumerable.Empty<DBBackup>();
            }

        }

        public async Task<IEnumerable<DBBackup>> GetBackups(DateTime from, DateTime to)
        {
            try
            {
                var Q = TblBackups.Between(from, to).OrderBy().OptArg("index", R.Desc("startdate"));
                return await Q.RunCursorAsync<DBBackup>(Conn);
            }
            catch (Exception e)
            {
                return Enumerable.Empty<DBBackup>();
            }

        }

        public async Task<IEnumerable<DBBackup>> GetBackups(Guid server)
        {
            try
            {
                var Q = TblBackups.GetAll(server).OptArg("index", "server").OrderBy(R.Desc("startdate")).Limit(10);
                return await Q.RunAtomAsync<List<DBBackup>>(Conn);
            }
            catch (Exception e)
            {
                return Enumerable.Empty<DBBackup>();
            }
        }

        public Task<DBBackup> GetBackup(Guid backupId)
        {
            var Q = TblBackups.Get(backupId);

            return Q.RunAtomAsync<DBBackup>(Conn);
        }

        public async Task<IEnumerable<DBVMwareVM>> GetVMwareVM(Guid backup)
        {
            var Q = TblVMwareVM.GetAll(backup).OptArg("index", "backup");

            return await Q.RunCursorAsync<DBVMwareVM>(Conn);
        }

        public async Task<IEnumerable<DBVMwareVM>> GetServerVMwareVM(Guid server)
        {
            var Q = TblVMwareVM.GetAll(server).OptArg("index", "server");

            return await Q.RunCursorAsync<DBVMwareVM>(Conn);
        }

        public async Task<IEnumerable<DBFile>> GetFiles(Guid backup)
        {
            var Q = TblFiles.GetAll(backup).OptArg("index", "backup");
            return await Q.RunCursorAsync<DBFile>(Conn);
        }

        public async Task<IEnumerable<DBFile>> GetFiles(Guid backup, string folder)
        {
            if (string.IsNullOrWhiteSpace(folder))
                return Enumerable.Empty<DBFile>();

            var Q = TblFiles.GetAll(backup).OptArg("index", "backup")
                .Filter(file => file["name"].Match(R.Expr("(?i)^" + folder.Replace("\\", "\\\\"))));

            var files = await Q.RunCursorAsync<DBFile>(Conn);

            return files.Where(file =>
            {
                return file.Name.IndexOf('\\', folder.Length + 1) < 0;
            });
        }

        /*
        public async Task<IEnumerable<DBFolder>> GetFolders(Guid backup)
        {
            var Q = TblFolders.GetAll(backup).OptArg("index", "backup");
            return await Q.RunCursorAsync<DBFolder>(Conn);
        }
        */

        public Task<DBVMwareVM> GetLatestVM(Guid server, string vmMoref)
        {
            var backup_vm = TblBackups
                .GetAll(server).OptArg("index", "server")["id"]
                .Map(id => R.Array(id, vmMoref)).CoerceTo("array");
            var Q = TblVMwareVM
                .GetAll(R.Args(backup_vm)).OptArg("index", "backup_vm")
                .Filter(row => row["valid"].Eq(true))
                .Max("startdate").Default_((Javascript)null);

            return Q.RunAtomAsync<DBVMwareVM>(Conn);
        }

        public async Task<DBVMDisk> GetVMDisk(Guid vm, int key)
        {
            var a = new RethinkDb.Driver.Model.Arguments((object)new object[] { vm, key });
            var Q = TblVMDisks.GetAll(a).OptArg("index", "vm_disk");

            var r = await Q.RunCursorAsync<DBVMDisk>(Conn);
            return r.FirstOrDefault();
        }

        public Task<DBFile> GetFile(Guid file)
        {
            var Q = TblFiles.Get(file);
            return Q.RunAtomAsync<DBFile>(Conn);
        }

        public Task<DBFolder> GetFolder(Guid folder)
        {
            var Q = TblFolders.Get(folder);
            return Q.RunAtomAsync<DBFolder>(Conn);
        }

        /*
        public async Task<IEnumerable<string>> GetFolders(Guid backup, string pre = "")
        {

            var Q = TblFolders.GetAll(backup).OptArg("index", "backup")
                .Filter(folder => folder["name"].Match(R.Expr("(?i)^" + pre.Replace("\\", "\\\\"))));

            try
            {
                var folders = await Q.RunCursorAsync<DBFolder>(Conn);
                var result = folders.Select(folder =>
                {
                    var left = folder.Name.Substring(pre == null ? 0 : pre.Length);
                    var split = left.Split('\\');
                    if (split.Length > 1 && split[0] == "")
                        return pre + "\\" + split[1];
                    return pre + split[0];

                }).Distinct();

                return result;

            }
            catch (Exception e)
            {
                throw e;
            }
        }
*/


        public async Task<IEnumerable<DBFolder>> GetFolders(Guid backup, string pre = "")
        {

            if (string.IsNullOrEmpty(pre))
            {
                var Q = TblFolders.GetAll(backup).OptArg("index", "backup")
                    .Min(folder => folder["name"].Split("\\").Count());

                try
                {
                    var folder = await Q.RunAtomAsync<DBFolder>(Conn);
                    return new List<DBFolder> { folder };
                }
                catch (Exception e)
                {
                    throw e;
                }
            }

            else
            {
                var Q = TblFolders.GetAll(backup).OptArg("index", "backup")
                    .Filter(folder => folder["name"].Match(R.Expr("(?i)^" + pre.Replace("\\", "\\\\"))));

                try
                {
                    var folders = await Q.RunCursorAsync<DBFolder>(Conn);
                    return folders.Where(folder =>
                    {
                        var right = folder.Name.Substring(pre.Length);

                        return right.Split('\\').Length == 2;

                    });

                }
                catch (Exception e)
                {
                    throw e;
                }


            }

        }

        public async Task<Guid> AddServer(DBServer server)
        {
            try
            {
                var Q = TblServers.Insert(server);
                var result = await Q.RunResultAsync(Conn);
                return result.GeneratedKeys[0];
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        public async Task<Guid> AddCalendarEntry(DBCalendarEntry entry)
        {
            try
            {
                var Q = TblCalendar.Insert(entry);
                var result = await Q.RunResultAsync(Conn);
                return result.GeneratedKeys[0];
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        public async Task<DBCalendarEntry> GetCalendarEntry(Guid entry)
        {
            var Q = TblCalendar.Get(entry);
            return await Q.RunAtomAsync<DBCalendarEntry>(Conn);
        }

        public async Task<IEnumerable<DBCalendarEntry>> GetServerCalendar(Guid server)
        {
            var Q = TblCalendar.GetAll(server).OptArg("index", "server");//.Filter(row => row["nextrun"].During(row["lastrun"], R.Now()));
            return await Q.RunCursorAsync<DBCalendarEntry>(Conn);
        }

        public async Task<IEnumerable<DBCalendarEntry>> GetNextEntries()
        {
            var Q = TblCalendar.Filter(row => row["nextrun"].Le(DateTime.UtcNow) && row["enabled"]);
            return await Q.RunCursorAsync<DBCalendarEntry>(Conn);
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    try
                    {
                        conn.Dispose();
                    }
                    catch { }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~RethinkDBAccess() {
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
