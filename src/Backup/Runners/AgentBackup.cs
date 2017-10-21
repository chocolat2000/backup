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

namespace Backup.Runners
{
    public class AgentBackup : IBackupServiceCallback
    {
        private IMetaDBAccess metaDB;
        private IBackupService backupServiceProxy;
        private IStreamService streamServiceProxy;

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

        public async Task Run(Guid serverId, string[] items)
        {
            if (backup != null)
                return;

            var server = await metaDB.GetWindowsServer(serverId);

            var backupTcpBinding = new NetTcpBinding();
            var streamTcpBinding = new NetTcpBinding(SecurityMode.None);
            streamTcpBinding.TransferMode = TransferMode.StreamedResponse;
            streamTcpBinding.ReceiveTimeout = TimeSpan.FromMinutes(30);
            streamTcpBinding.SendTimeout = TimeSpan.FromMinutes(30);
            //streamTcpBinding.OpenTimeout = TimeSpan.FromMinutes(30);
            //streamTcpBinding.CloseTimeout = TimeSpan.FromMinutes(30);
            streamTcpBinding.MaxBufferSize = 65536;
            streamTcpBinding.MaxReceivedMessageSize = 10995116277760; // 10To

            var backupFactory = new DuplexChannelFactory<IBackupService>(new InstanceContext(this), backupTcpBinding, new EndpointAddress("net.tcp://localhost:8733/Backup/"));
            var streamFactory = new ChannelFactory<IStreamService>(streamTcpBinding, new EndpointAddress("net.tcp://localhost:8734/Streaming/"));

            backupServiceProxy = backupFactory.CreateChannel();
            streamServiceProxy = streamFactory.CreateChannel();

            backup = new DBBackup { Server = serverId, StartDate = DateTime.Now.ToUniversalTime(), Status = Status.Running, Log = new List<string>() };
            backup.Id = await metaDB.AddBackup(backup);
            backupStatus = Status.Successful;

            try
            {
                await backupServiceProxy.BackupAsync(items, backup.Id);
            }
            catch (EndpointNotFoundException)
            {
                backup.Status = Status.Failed;
                backup.EndDate = DateTime.Now;
                backup.AppendLog(string.Format("Cannot connect to server {0} ({1})", server.Name, server.Ip));
                await metaDB.AddBackup(backup);
                return;
            }

            cancelSource = new CancellationTokenSource();

            while (true)
            {
                BackupItem item = null;

                if (cancelSource.IsCancellationRequested)
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
                        item = itemsQueue.Take(cancelSource.Token);
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
                await backupServiceProxy.BackupCompleteAsync(backup.Id);
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

            cancelSource.Dispose();
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
                var stream = await streamServiceProxy.GetStreamAsync(file.StreamGuid);

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
                var readCount = await stream.ReadAsync(buffer, 0, buffer.Length);

                while (readCount > 0)
                {
                    newbLocks = hasher.NextBlock(buffer, 0, readCount);
                    foreach (var newbLock in newbLocks)
                    {
                        var blockGuid = await BlocksManager.AddBlockToDB(newbLock);
                        await metaDB.AddFileBlock(new DBFileBlock { Block = blockGuid, File = currentFileId, Offset = currentFilePos });
                        currentFilePos += newbLock.Length;
                        totalBlocks++;
                    }
                    readCount = await stream.ReadAsync(buffer, 0, buffer.Length);
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
