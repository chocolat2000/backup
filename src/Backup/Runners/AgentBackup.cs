using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Backup.Services;
using BackupDatabase;
using BackupDatabase.Models;
using BackupNetworkLibrary.Model;
using AgentProxy;

namespace Backup.Runners
{
    public class AgentBackup : IBackupServiceCallback, IDisposable
    {
        private IMetaDBAccess metaDB;
        private WindowsProxy agentProxy;

        private BlockingCollection<BackupItem> itemsQueue = new BlockingCollection<BackupItem>(new ConcurrentQueue<BackupItem>());
        private CancellationTokenSource ctokenCancelBackup = new CancellationTokenSource();
        private CancellationToken ctoken;

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

        public async Task Run(Guid serverId, string[] items, CancellationToken ctoken)
        {
            if (backup != null)
                return;

            this.ctoken = ctoken;

            var server = await metaDB.GetWindowsServer(serverId, withcreds: true);

            agentProxy = new WindowsProxy(server.Ip, server.Username, server.Password)
            {
                BackupServiceCallback = this
            };

            backup = new DBBackup { Server = serverId, StartDate = DateTime.Now.ToUniversalTime(), Status = Status.Running, Log = new List<string>() };
            backup.Id = await metaDB.AddBackup(backup);
            backupStatus = Status.Successful;

            IObserver<CancellationTokenSource> observer = default;

            Observable.Interval(TimeSpan.FromSeconds(5)).TakeUntil(
                Observable.Create<CancellationTokenSource>(o =>
                {
                    observer = o;
                    return () => { };
                }))
                .Subscribe(_ =>
                {
                    var b = metaDB.GetBackup(backup.Id).GetAwaiter().GetResult();
                    if (b.Status == Status.Cancelled)
                    {
                        ctokenCancelBackup.Cancel();
                        observer.OnNext(ctokenCancelBackup);
                    }
                });

            try
            {
                await agentProxy.Backup(items, backup.Id);

                foreach (var item in itemsQueue.GetConsumingEnumerable(ctoken))
                {
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

            }
            catch (EndpointNotFoundException)
            {
                backup.AppendLog($"Cannot connect to server {server.Name} [{server.Ip}]");
                backupStatus = Status.Failed;
            }
            catch (OperationCanceledException)
            {
                backupStatus = Status.Cancelled;
            }
            catch (Exception e)
            {
                backup.AppendLog($"General Error: {e.Message}");
                backupStatus = Status.Failed;
            }
            finally
            {
                try
                {
                    await agentProxy.BackupComplete(backup.Id);
                }
                catch (Exception) { }

                backup.Status = backupStatus;
                backup.EndDate = DateTime.Now.ToUniversalTime();
                await metaDB.AddBackup(backup);
            }

            Console.WriteLine($"{DateTime.Now} - Backup ended");
            Console.WriteLine($"{DateTime.Now} - Status: {backup.Status}");

            backup = null;
        }

        private void CheckCancelStatus()
        {
            ctoken.ThrowIfCancellationRequested();
            ctokenCancelBackup.Token.ThrowIfCancellationRequested();
        }

        #region WCF Callback implementation
        public void SendBackupItem(BackupItem item)
        {
            itemsQueue.Add(item);
        }

        public void SendWarningLog(string message)
        {
            backup.AppendLog(message);
            backupStatus = Status.Warning;
        }

        public void SendBackupCompleted()
        {
            itemsQueue.CompleteAdding();
        }
        #endregion

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

                Console.WriteLine($"{DateTime.Now} - Backup file: {file.Name}");

                IList<byte[]> newbLocks;
                var readCount = await stream.ReadAsync(buffer, 0, buffer.Length, ctoken);

                while (readCount > 0)
                {
                    CheckCancelStatus();

                    newbLocks = hasher.NextBlock(buffer, 0, readCount);
                    foreach (var newbLock in newbLocks)
                    {
                        CheckCancelStatus();
                        var blockGuid = await BlocksManager.AddBlockToDB(newbLock);
                        await metaDB.AddFileBlock(new DBFileBlock { Block = blockGuid, File = currentFileId, Offset = currentFilePos });
                        currentFilePos += newbLock.Length;
                        totalBlocks++;
                    }
                    readCount = await stream.ReadAsync(buffer, 0, buffer.Length, ctoken);
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
            catch(OperationCanceledException e)
            {
                // re-throws OperationCanceled exceptions
                throw e;
            }
            catch (Exception e)
            {
                backup.AppendLog($"Error file: {file.Name} --- {e.Message} ");
                backupStatus = Status.Warning;
                await metaDB.AddBackup(backup); // Update backup with error
            }


            Console.WriteLine($"{DateTime.Now} - Total blocks: {totalBlocks}");
            Console.WriteLine($"{DateTime.Now} - DB blocks: {BlocksManager.dbBlocks}");

        }


        private async Task BackupFolder(BackupItem folder)
        {
            await metaDB.AddFolder(new DBFolder { Name = folder.Name, Backup = backup.Id, LastWriteTime = folder.LastWriteTime });
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    itemsQueue.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~AgentBackup() {
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
