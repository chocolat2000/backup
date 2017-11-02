using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using BackupNetworkLibrary.Model;


namespace AgentProxy
{
    class BackupServiceClient : DuplexClientBase<IBackupService>, IBackupService
    {
        public BackupServiceClient(InstanceContext callbackInstance) :
            base(callbackInstance)
        {
        }

        public BackupServiceClient(InstanceContext callbackInstance, string endpointConfigurationName) :
            base(callbackInstance, endpointConfigurationName)
        {
        }

        public BackupServiceClient(InstanceContext callbackInstance, string endpointConfigurationName, string remoteAddress) :
            base(callbackInstance, endpointConfigurationName, remoteAddress)
        {
        }

        public BackupServiceClient(InstanceContext callbackInstance, string endpointConfigurationName, EndpointAddress remoteAddress) :
            base(callbackInstance, endpointConfigurationName, remoteAddress)
        {
        }

        public BackupServiceClient(InstanceContext callbackInstance, Binding binding, EndpointAddress remoteAddress) :
            base(callbackInstance, binding, remoteAddress)
        {
        }

        public void Backup(string[] items, Guid id)
        {
            Channel.Backup(items, id);
        }

        public Task BackupAsync(string[] items, Guid id)
        {
            return Channel.BackupAsync(items, id);
        }

        public void BackupComplete(Guid id)
        {
            Channel.BackupComplete(id);
        }

        public Task BackupCompleteAsync(Guid id)
        {
            return Channel.BackupCompleteAsync(id);
        }
    }
}
