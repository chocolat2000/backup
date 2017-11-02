using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using BackupNetworkLibrary.Model;

namespace AgentProxy
{
    class GeneralServiceClient : ClientBase<IGeneralService>, IGeneralService
    {
        public GeneralServiceClient()
        {
        }

        public GeneralServiceClient(string endpointConfigurationName) : 
                base(endpointConfigurationName)
        {
        }

        public GeneralServiceClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress)
        {
        }

        public GeneralServiceClient(string endpointConfigurationName, EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress)
        {
        }

        public GeneralServiceClient(Binding binding, EndpointAddress remoteAddress) : 
                base(binding, remoteAddress)
        {
        }

        public FolderContent GetContent(string folder)
        {
            return Channel.GetContent(folder);
        }

        public Task<FolderContent> GetContentAsync(string folder)
        {
            return Channel.GetContentAsync(folder);
        }

        public string[] GetDrives()
        {
            return Channel.GetDrives();
        }

        public Task<string[]> GetDrivesAsync()
        {
            return Channel.GetDrivesAsync();
        }
    }
}
