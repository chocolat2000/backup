using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.Serialization;

/*
namespace System.Runtime.Serialization
{
    public class ExtensionDataObject
    {
    }

    internal interface IExtensibleDataObject
    {
    }
}
*/

namespace AgentProxy
{
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName = "IGeneralService")]
    public interface IGeneralService
    {

        [System.ServiceModel.OperationContractAttribute(Name = "GetDrives")]
        string[] GetDrives();

        [System.ServiceModel.OperationContractAttribute(Name = "GetDrives")]
        System.Threading.Tasks.Task<string[]> GetDrivesAsync();

        [System.ServiceModel.OperationContractAttribute(Name = "GetContent")]
        BackupNetworkLibrary.Model.FolderContent GetContent(string folder);

        [System.ServiceModel.OperationContractAttribute(Name = "GetContent")]
        System.Threading.Tasks.Task<BackupNetworkLibrary.Model.FolderContent> GetContentAsync(string folder);
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName = "IBackupService", CallbackContract = typeof(IBackupServiceCallback))]
    public interface IBackupService
    {

        [System.ServiceModel.OperationContractAttribute(IsOneWay = true, Action = "http://tempuri.org/IBackupService/Backup")]
        void Backup(string[] items, System.Guid id);

        [System.ServiceModel.OperationContractAttribute(IsOneWay = true, Action = "http://tempuri.org/IBackupService/Backup")]
        System.Threading.Tasks.Task BackupAsync(string[] items, System.Guid id);

        [System.ServiceModel.OperationContractAttribute(IsOneWay = true, Action = "http://tempuri.org/IBackupService/BackupComplete")]
        void BackupComplete(System.Guid id);

        [System.ServiceModel.OperationContractAttribute(IsOneWay = true, Action = "http://tempuri.org/IBackupService/BackupComplete")]
        System.Threading.Tasks.Task BackupCompleteAsync(System.Guid id);
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IBackupServiceCallback
    {

        [System.ServiceModel.OperationContractAttribute(IsOneWay = true, Action = "http://tempuri.org/IBackupService/SendBackupItem")]
        void SendBackupItem(BackupNetworkLibrary.Model.BackupItem item);

        [System.ServiceModel.OperationContractAttribute(IsOneWay = true, Action = "http://tempuri.org/IBackupService/SendWarningLog")]
        void SendWarningLog(string message);

        [System.ServiceModel.OperationContractAttribute(IsOneWay = true, Action = "http://tempuri.org/IBackupService/SendBackupCompleted")]
        void SendBackupCompleted();
    }

    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName = "IStreamService")]
    public interface IStreamService
    {

        [System.ServiceModel.OperationContractAttribute(Action = "http://tempuri.org/IStreamService/GetStream", ReplyAction = "http://tempuri.org/IStreamService/GetStreamResponse")]
        System.IO.Stream GetStream(System.Guid id);

        [System.ServiceModel.OperationContractAttribute(Action = "http://tempuri.org/IStreamService/GetStream", ReplyAction = "http://tempuri.org/IStreamService/GetStreamResponse")]
        System.Threading.Tasks.Task<System.IO.Stream> GetStreamAsync(System.Guid id);
    }

}
