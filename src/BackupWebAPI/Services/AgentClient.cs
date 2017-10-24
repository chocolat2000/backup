using Backup.Services;
using BackupDatabase;
using BackupNetworkLibrary.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Threading.Tasks;

namespace BackupWeb.Services
{
    public class AgentClient
    {
        private readonly IDictionary<Guid, ChannelFactory<IGeneralService>> windowsGeneralConnections = new Dictionary<Guid, ChannelFactory<IGeneralService>>();
        private readonly IMetaDBAccess metaDB;

        public AgentClient(IMetaDBAccess metaDB)
        {
            this.metaDB = metaDB;
        }

        private IGeneralService GetGeneralService(Guid id)
        {
            ChannelFactory<IGeneralService> backupFactory;
            lock (windowsGeneralConnections)
            {
                if (!windowsGeneralConnections.TryGetValue(id, out backupFactory) || (backupFactory.State != CommunicationState.Opened))
                {
                    var server = metaDB.GetWindowsServerSync(id);
                    if (server == null) return null;
                    var backupTcpBinding = new NetTcpBinding
                    {
                        MaxReceivedMessageSize = int.MaxValue,
                    };
                    backupTcpBinding.Security.Mode = SecurityMode.Transport;
                    backupTcpBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;
                    backupFactory = new ChannelFactory<IGeneralService>(backupTcpBinding, new EndpointAddress($"net.tcp://{server.Ip}:8733/General/"));
                    backupFactory.Credentials.Windows.ClientCredential = new NetworkCredential(server.Username, server.Password);

                    backupFactory.Faulted += BackupFactory_Faulted;

                    windowsGeneralConnections.TryAdd(id, backupFactory);
                }
            }

            return backupFactory.CreateChannel();
        }

        private void BackupFactory_Faulted(object sender, EventArgs e)
        {
            //??
        }

        public async Task<string[]> GetDrives(Guid id)
        {
            var serverProxy = GetGeneralService(id);
            try
            {
                return await serverProxy.GetDrivesAsync();
            }
            catch(Exception e)
            {
                return null;
            }
        }

        public async Task<FolderContent> GetContent(Guid id, string folder)
        {
            var serverProxy = GetGeneralService(id);
            try
            {
                return await serverProxy.GetContentAsync(folder);
            }
            catch(Exception e)
            {
                return null;
            }
        }
    }
}
