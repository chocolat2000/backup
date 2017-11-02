using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BackupDatabase.Models;

namespace BackupDatabase
{
    public interface IMetaDBAccess : IDisposable
    {
        Task<IEnumerable<DBServer>> GetServers();

        Task<IEnumerable<DBWindowsServer>> GetWindowsServers();

        Task<IEnumerable<DBVMwareServer>> GetVMwareServers();

        Task<ServerType> GetServerType(Guid id);

        Task<DBWindowsServer> GetWindowsServer(Guid id, bool withcreds = false);

        DBWindowsServer GetWindowsServerSync(Guid id, bool withcreds = false);

        Task<DBVMwareServer> GetVMWareServer(Guid id, bool withcreds = false);

        DBVMwareServer GetVMWareServerSync(Guid id, bool withcreds = false);

        Task<Guid> AddBackup(DBBackup backup);

        Task CancelBackup(Guid backupId);

        Task<long> BackupSize(Guid backupId);

        Task AddFileBlock(DBFileBlock fileBlock);

        Task AddFileBlocks(IEnumerable<DBFileBlock> fileBlocks);

        Task<IEnumerable<DBFileBlock>> GetFileBlocks(Guid fileId);

        Task AddVMDiskBlock(DBVMDiskBlock vmDiskBlock);

        Task AddVMDiskBlocks(IEnumerable<DBVMDiskBlock> vmDiskBlocks);

        Task<IEnumerable<DBVMDiskBlock>> GetVMDiskBlocks(Guid vmDiskId);

        Task CopyVMDiskBlocks(Guid sourceDiskId, Guid destinationDiskId);

        Task<long> GetPreviousVMDiskOffset(Guid diskId, long offset);

        Task<long> GetNextVMDiskOffset(Guid diskId, long offset);

        Task<Guid> AddFile(DBFile file);

        Task<Guid> AddFolder(DBFolder folder);

        Task<Guid> AddVMwareVM(DBVMwareVM vm);

        Task<Guid> AddVMDisk(DBVMDisk disk);

        Task<DBBackup> GetBackup(Guid backupId);

        Task<IEnumerable<DBBackup>> GetBackups();

        Task<IEnumerable<DBBackup>> GetBackups(DateTime from);

        Task<IEnumerable<DBBackup>> GetBackups(DateTime from, DateTime to);

        Task<IEnumerable<DBBackup>> GetBackups(Guid server);

        Task<IEnumerable<DBVMwareVM>> GetVMwareVM(Guid backup);

        Task<IEnumerable<DBVMwareVM>> GetServerVMwareVM(Guid server);

        Task<DBFile> GetFile(Guid file);

        Task<IEnumerable<DBFile>> GetFiles(Guid backup);

        Task<IEnumerable<DBFile>> GetFiles(Guid backup, string folder);

        Task<DBVMwareVM> GetLatestVM(Guid server, string vmMoref);

        Task<DBVMDisk> GetVMDisk(Guid vm, int key);

        Task<DBFolder> GetFolder(Guid folder);

        Task<IEnumerable<DBFolder>> GetFolders(Guid backup, string pre = "");

        Task<Guid> AddServer(DBServer server);

        Task UpdateServer(DBServer server);

        Task DeleteServer(Guid server);

        Task<Guid> AddCalendarEntry(DBCalendarEntry entry);

        Task<DBCalendarEntry> GetCalendarEntry(Guid entry);

        Task<IEnumerable<DBCalendarEntry>> GetServerCalendar(Guid server);

        Task<IEnumerable<DBCalendarEntry>> GetNextEntries();

    }
}
