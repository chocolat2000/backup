using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Backup.Services;
using BackupDatabase;
using BackupDatabase.Models;
using BackupNetworkLibrary.Model;
using AgentProxy;

namespace Backup.Runners
{
    public class AgentBackup : IBackupServiceCallback
    {
        private IMetaDBAccess metaDB;
        private WindowsProxy agentProxy;

        private BlockingCollection<BackupItem> itemsQueue = new BlockingCollection<BackupItem>(new ConcurrentQueue<BackupItem>());
        private CancellationTokenSource cancelSource;

        private BlockSplitter hasher;
        private DBBackup backup;
        private Status backupStatus;
        private Guid currentFileId;

        public AgentBackup(IMetaDBAccess metaDB)
        {
            this.metaDB = metaDB;
            hasher = new BlockSplitter();

            backup = null;
        }

        public async Task Run(Guid serverId, string[] items, CancellationTokenSource cancelSource = null)
        {
            if (backup != null)
                return;

            if (cancelSource == null)
                this.cancelSource = new CancellationTokenSource();
            else
                this.cancelSource = cancelSource;

            var server = await metaDB.GetWindowsServer(serverId);

            agentProxy = new WindowsProxy(server.Ip, server.Username, server.Password)
            {
                BackupServiceCallback = this
            };

            backup = new DBBackup { Server = serverId, StartDate = DateTime.Now.ToUniversalTime(), Status = Status.Running, Log = new List<string>() };
            backup.Id = await metaDB.AddBackup(backup);
            backupStatus = Status.Successful;

            try
            {
                await agentProxy.Backup(items, backup.Id);
            }
            catch (EndpointNotFoundException)
            {
                backup.Status = Status.Failed;
                backup.EndDate = DateTime.Now;
                backup.AppendLog(string.Format("Cannot connect to server {0} ({1})", server.Name, server.Ip));
                await metaDB.AddBackup(backup);
                return;
            }

           
            while (true)
            {
                BackupItem item = null;

                if (this.cancelSource.IsCancellationRequested)
                {
                    if (itemsQueue.Count > 0)
                    {
                        item = itemsQueue.Take();
                    }
                    else
                        break;
                }
                else
                {
                    try
                    {
                        item = itemsQueue.Take(this.cancelSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        if (itemsQueue.Count > 0)
                        {
                            item = itemsQueue.Take();
                        }
                        else
                            break;
                    }
                }

                switch (item.Type)
                {
                    case BackupItemType.File:
                        {
                            await BackupFile(item);
                        }
                        break;
                    case BackupItemType.Folder:
                        {
                            await BackupFolder(item);
                        }
                        break;
                }

            }

            try
            {
                await agentProxy.BackupComplete(backup.Id);
            }
            catch (Exception)
            {

            }
            finally
            {
                backup.Status = backupStatus;
                backup.EndDate = DateTime.Now.ToUniversalTime();
                await metaDB.AddBackup(backup);
            }

            this.cancelSource.Dispose();
            backup = null;
        }

        // WCF Callback implementation
        public void SendBackupItem(BackupItem item)
        {
            itemsQueue.Add(item);

        }

        public void BackupCompleted()
        {
            cancelSource.Cancel();
        }

        public void SendWarningLog(string message)
        {
            backup.AppendLog(message);
            backupStatus = Status.Warning;
        }

        private async Task BackupFile(BackupItem file)
        {

            var totalBlocks = 0;
            try
            {
                var stream = await agentProxy.GetStream(file.StreamGuid);

                hasher.Initialize();

                var dbFile = new DBFile { Name = file.Name, Backup = backup.Id, LastWriteTime = file.LastWriteTime, Length = file.Length, Valid = false };
                currentFileId = await metaDB.AddFile(dbFile);
                dbFile.Id = currentFileId;

                var currentFilePos = 0L;
                var buffer = new byte[1024 * 64];
                BlocksManager.dbBlocks = 0;

                Console.WriteLine(DateTime.Now);
                Console.WriteLine("Backup file : " + file.Name);

                IList<byte[]> newbLocks;
                var readCount = await stream.ReadAsync(buffer, 0, buffer.Length, cancelSource.Token);

                while (readCount > 0)
                {
                    if(cancelSource.IsCancellationRequested)
                    {
                        throw new Exception("Operation Cancelled");
                    }

                    newbLocks = hasher.NextBlock(buffer, 0, readCount);
                    foreach (var newbLock in newbLocks)
                    {
                        var blockGuid = await BlocksManager.AddBlockToDB(newbLock);
                        await metaDB.AddFileBlock(new DBFileBlock { Block = blockGuid, File = currentFileId, Offset = currentFilePos });
                        currentFilePos += newbLock.Length;
                        totalBlocks++;
                    }
                    readCount = await stream.ReadAsync(buffer, 0, buffer.Length, cancelSource.Token);
                }

                if (hasher.HasRemainingBytes)
                {
                    var lastLock = hasher.RemainingBytes();
                    var blockGuid = await BlocksManager.AddBlockToDB(lastLock);
                    await metaDB.AddFileBlock(new DBFileBlock { Block = blockGuid, File = currentFileId, Offset = currentFilePos });
                    currentFilePos += lastLock.Length;
                    totalBlocks++;
                }

                dbFile.Valid = true;
                await metaDB.AddFile(dbFile);
            }
            catch (Exception e)
            {
                backup.AppendLog(string.Format("Error file : {0} --- {1} ", file.Name, e.Message));
                backupStatus = Status.Warning;
                await metaDB.AddBackup(backup); // Update backup with error
            }



            Console.WriteLine(DateTime.Now);

            Console.WriteLine("Total blocks : " + totalBlocks);
            Console.WriteLine("DB blocks : " + BlocksManager.dbBlocks);


        }


        private async Task BackupFolder(BackupItem folder)
        {
            await metaDB.AddFolder(new DBFolder { Name = folder.Name, Backup = backup.Id, LastWriteTime = folder.LastWriteTime });
        }

    }

}
