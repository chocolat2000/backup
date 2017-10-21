using BackupNetworkLibrary.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace BackupService.Services
{

    [ServiceContract]
    public interface IGeneralService
    {
        [OperationContract]
        IEnumerable<string> GetDrives();

        [OperationContract]
        FolderContent GetContent(string folder);

    }

    [ServiceContract(CallbackContract = typeof(IBackupServiceCallback))]
    public interface IBackupService
    {
        [OperationContract(IsOneWay = true)]
        void Backup(IEnumerable<string> items, Guid id);

        [OperationContract(IsOneWay = true)]
        void BackupComplete(Guid id);
    }

    [ServiceContract]
    public interface IBackupServiceCallback
    {
        [OperationContract(IsOneWay = true)]
        void SendBackupItem(BackupItem item);

        [OperationContract(IsOneWay = true)]
        void SendWarningLog(string message);

        [OperationContract(IsOneWay = true)]
        void BackupCompleted();
    }

    [ServiceContract]
    public interface IStreamService
    {
        [OperationContract]
        Stream GetStream(Guid id);
    }
}
