using Alphaleonis.Win32.Filesystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;
using BackupNetworkLibrary.Model;
using BackupService.Commands;
using System.Collections.Concurrent;
using System.Security.Permissions;
using System.Security.Principal;

namespace BackupService.Services
{

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class GeneralService : IGeneralService
    {
        [OperationBehavior]
        [PrincipalPermission(SecurityAction.Demand)]
        public IEnumerable<string> GetDrives()
        {
            return DriveInfo.GetDrives().Where(drive => drive.DriveType == System.IO.DriveType.Fixed).Select(driveInfo => driveInfo.Name);
        }

        [OperationBehavior]
        [PrincipalPermission(SecurityAction.Demand)]
        public FolderContent GetContent(string folder)
        {
            var content = new FolderContent
            {
                Folders = Directory.EnumerateDirectories(folder),
                Files = Directory.EnumerateFiles(folder)
            };
            return content;
        }

    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class BackupService : IBackupService
    {
        private ConcurrentDictionary<Guid, BackupCommand> backups = new ConcurrentDictionary<Guid, BackupCommand>();
        private ConcurrentDictionary<Guid, string> files;

        public BackupService(ConcurrentDictionary<Guid, string> files)
        {
            this.files = files;
        }

        [OperationBehavior]
        [PrincipalPermission(SecurityAction.Demand)]
        public void Backup(IEnumerable<string> items, Guid backupId)
        {
            var callback = OperationContext.Current.GetCallbackChannel<IBackupServiceCallback>();
            var backup = new BackupCommand
            {
                SendBackupItem = item => callback.SendBackupItem(item),
                SendWarningLog = message => callback.SendWarningLog(message),
                SendBackupComplete = callback.SendBackupCompleted
            };
            backup.Initialize(files);

            backups.TryAdd(backupId, backup);
            backup.RunBackup(items);

        }

        [OperationBehavior]
        [PrincipalPermission(SecurityAction.Demand)]
        public void BackupComplete(Guid id)
        {
            if (backups.TryRemove(id, out BackupCommand backup))
                backup.BackupComplete();
        }
    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class StreamService : IStreamService
    {
        private ConcurrentDictionary<Guid, string> files;

        public StreamService(ConcurrentDictionary<Guid, string> files)
        {
            this.files = files;
        }

        public System.IO.Stream GetStream(Guid id)
        {
            System.IO.Stream stream = null;

            if (files.TryRemove(id, out string file))
            {
                try
                {
                    stream = File.OpenRead(file);
                    OperationContext.Current.OperationCompleted += (sender, args) =>
                    {
                        if (stream != null)
                            stream.Dispose();
                    };
                }
                catch (Exception ex)
                {
                    throw new FaultException(ex.Message);
                }
            }
            else
            {
                throw new FaultException($"Cannot find stream with id: {id}");
            }

            return stream;
        }
    }

}
