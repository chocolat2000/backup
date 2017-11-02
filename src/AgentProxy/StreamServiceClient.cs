using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace AgentProxy
{
    class StreamServiceClient : ClientBase<IStreamService>, IStreamService
    {
        public StreamServiceClient()
        {
        }

        public StreamServiceClient(string endpointConfigurationName) : 
                base(endpointConfigurationName)
        {
        }

        public StreamServiceClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress)
        {
        }

        public StreamServiceClient(string endpointConfigurationName, EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress)
        {
        }

        public StreamServiceClient(Binding binding, EndpointAddress remoteAddress) : 
                base(binding, remoteAddress)
        {
        }

        public Stream GetStream(Guid id)
        {
            return Channel.GetStream(id);
        }

        public Task<Stream> GetStreamAsync(Guid id)
        {
            return Channel.GetStreamAsync(id);
        }
    }
}
