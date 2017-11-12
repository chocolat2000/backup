using BackupNetworkLibrary.Model;
using System;
using System.IO;
using System.Net;
using System.ServiceModel;
using System.Threading.Tasks;
using AgentProxy.Exceptions;
using System.ServiceModel.Security;

namespace AgentProxy
{
    public class WindowsProxy : IDisposable
    {
        private readonly string server;
        private readonly NetworkCredential networkCredential;

        public IBackupServiceCallback BackupServiceCallback { get; set; }

        private GeneralServiceClient generalClient;
        private GeneralServiceClient GeneralClient
        {
            get
            {
                if (generalClient == null)
                {
                    var generalTcpBinding = new NetTcpBinding(SecurityMode.Transport)
                    {
                        MaxReceivedMessageSize = int.MaxValue,
                    };
                    generalTcpBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;

                    generalClient = new GeneralServiceClient(generalTcpBinding, new EndpointAddress($"net.tcp://{server}:8733/General/"));
                    generalClient.ClientCredentials.Windows.ClientCredential = networkCredential;
                }
                return generalClient;
            }
        }

        private BackupServiceClient backupClient;
        private BackupServiceClient BackupClient
        {
            get
            {
                if (backupClient == null)
                {
                    if(BackupServiceCallback == null)
                    {
                        throw new ArgumentException("You must provide an IBackupServiceCallback first", nameof(BackupServiceCallback));
                    }

                    var backupTcpBinding = new NetTcpBinding(SecurityMode.Transport)
                    {
                        MaxReceivedMessageSize = int.MaxValue,
                    };
                    backupTcpBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;

                    backupClient = new BackupServiceClient(new InstanceContext(BackupServiceCallback), backupTcpBinding, new EndpointAddress($"net.tcp://{server}:8733/Backup/"));
                    backupClient.ClientCredentials.Windows.ClientCredential = networkCredential;
                }
                return backupClient;
            }
        }

        private StreamServiceClient streamClient;
        private StreamServiceClient StreamClient
        {
            get
            {
                if (streamClient == null)
                {
                    var streamTcpBinding = new NetTcpBinding//(SecurityMode.Transport)
                    {
                        TransferMode = TransferMode.StreamedResponse,
                        ReceiveTimeout = TimeSpan.FromMinutes(30),
                        SendTimeout = TimeSpan.FromMinutes(30),
                        MaxBufferSize = 65536,
                        MaxReceivedMessageSize = int.MaxValue
                    };

                    //streamTcpBinding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Windows;

                    streamClient = new StreamServiceClient(streamTcpBinding, new EndpointAddress($"net.tcp://{server}:8734/Streaming/"));
                    //streamClient.ClientCredentials.Windows.ClientCredential = networkCredential;
                }
                return streamClient;
            }

        }

        public WindowsProxy(string server, string username, string password)
        {
            if (string.IsNullOrWhiteSpace(server))
                throw new ArgumentException("Argument cannot be null or empty", nameof(server));

            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Argument cannot be null or empty", nameof(username));

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Argument cannot be null or empty", nameof(password));

            this.server = server;
            networkCredential = new NetworkCredential(username, password);

        }

        public async Task<string[]> GetDrives()
        {
            try
            {
                return await GeneralClient.GetDrivesAsync();
            }
            catch(EndpointNotFoundException)
            {
                throw new AgentNotFoundException();
            }
            catch(SecurityNegotiationException)
            {
                throw new BadCredentialsException();
            }
            catch(CommunicationObjectFaultedException)
            {
                throw new CommunicationFaultedException();
            }
        }

        public async Task<FolderContent> GetContent(string folder)
        {
            try
            {
                return await GeneralClient.GetContentAsync(folder);
            }
            catch (EndpointNotFoundException)
            {
                throw new AgentNotFoundException();
            }
            catch (SecurityNegotiationException)
            {
                throw new BadCredentialsException();
            }
            catch (CommunicationObjectFaultedException)
            {
                throw new CommunicationFaultedException();
            }

        }

        public async Task Backup(string[] items, Guid id)
        {
            try
            {
                await BackupClient.BackupAsync(items, id);
            }
            catch (EndpointNotFoundException)
            {
                throw new AgentNotFoundException();
            }
            catch (SecurityNegotiationException)
            {
                throw new BadCredentialsException();
            }
            catch (CommunicationObjectFaultedException)
            {
                throw new CommunicationFaultedException();
            }


        }

        public async Task BackupComplete(Guid id)
        {
            try
            {
                await BackupClient.BackupCompleteAsync(id);
            }
            catch (EndpointNotFoundException)
            {
                throw new AgentNotFoundException();
            }
            catch (SecurityNegotiationException)
            {
                throw new BadCredentialsException();
            }
            catch (CommunicationObjectFaultedException)
            {
                throw new CommunicationFaultedException();
            }

        }

        public async Task<Stream> GetStream(Guid id)
        {
            try
            {
                return await StreamClient.GetStreamAsync(id);
            }
            catch (EndpointNotFoundException)
            {
                throw new AgentNotFoundException();
            }
            catch (SecurityNegotiationException)
            {
                throw new BadCredentialsException();
            }
            catch (CommunicationObjectFaultedException)
            {
                throw new CommunicationFaultedException();
            }

        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    (generalClient as IDisposable)?.Dispose();
                    (backupClient as IDisposable)?.Dispose();
                    (streamClient as IDisposable)?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Proxy() {
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
