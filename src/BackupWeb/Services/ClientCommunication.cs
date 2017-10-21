
namespace Backup.Services
{
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName = "IGeneralService")]
    public interface IGeneralService
    {

        [System.ServiceModel.OperationContractAttribute(Action = "http://tempuri.org/IGeneralService/GetDrives", ReplyAction = "http://tempuri.org/IGeneralService/GetDrivesResponse")]
        string[] GetDrives();

        [System.ServiceModel.OperationContractAttribute(Action = "http://tempuri.org/IGeneralService/GetDrives", ReplyAction = "http://tempuri.org/IGeneralService/GetDrivesResponse")]
        System.Threading.Tasks.Task<string[]> GetDrivesAsync();

        [System.ServiceModel.OperationContractAttribute(Action = "http://tempuri.org/IGeneralService/GetContent", ReplyAction = "http://tempuri.org/IGeneralService/GetContentResponse")]
        BackupNetworkLibrary.Model.FolderContent GetContent(string folder);

        [System.ServiceModel.OperationContractAttribute(Action = "http://tempuri.org/IGeneralService/GetContent", ReplyAction = "http://tempuri.org/IGeneralService/GetContentResponse")]
        System.Threading.Tasks.Task<BackupNetworkLibrary.Model.FolderContent> GetContentAsync(string folder);
    }

}
