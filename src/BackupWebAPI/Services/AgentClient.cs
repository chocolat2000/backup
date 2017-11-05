using BackupDatabase;
using BackupNetworkLibrary.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Threading.Tasks;
using AgentProxy;
using AgentProxy.Exceptions;

namespace BackupWeb.Services
{
    public class AgentClient
    {
        private readonly IDictionary<Guid, WindowsProxy> windowsProxies = new Dictionary<Guid, WindowsProxy>();
        private readonly IMetaDBAccess metaDB;

        public AgentClient(IMetaDBAccess metaDB)
        {
            this.metaDB = metaDB;
        }

        private WindowsProxy GetWindowsProxy(Guid id)
        {
            WindowsProxy proxy;
            lock (windowsProxies)
            {
                if (!windowsProxies.TryGetValue(id, out proxy))
                {
                    var server = metaDB.GetWindowsServerSync(id, true);
                    if (server == null) return null;
                    proxy = new WindowsProxy(server.Ip, server.Username, server.Password);
                    windowsProxies.Add(id, proxy);
                }
            }

            return proxy;
        }

        public async Task<string[]> GetDrives(Guid id)
        {
            var proxy = GetWindowsProxy(id);

            try
            {
                return await proxy.GetDrives();
            }
            catch (AgentNotFoundException)
            {
                windowsProxies.Remove(id);
                throw new Exception("Cannot connect to remote agent.");
            }
            catch (BadCredentialsException)
            {
                windowsProxies.Remove(id);
                throw new Exception("Remote agent refused credentials.");
            }
            catch (CommunicationFaultedException)
            {
                windowsProxies.Remove(id);
                throw new Exception("Communication with agent faulted.");
            }

        }

        public async Task<FolderContent> GetContent(Guid id, string folder)
        {
            var proxy = GetWindowsProxy(id);

            try
            {
                return await proxy.GetContent(folder);
            }
            catch (AgentNotFoundException)
            {
                windowsProxies.Remove(id);
                throw new Exception("Cannot connect to remote agent.");
            }
            catch (BadCredentialsException)
            {
                windowsProxies.Remove(id);
                throw new Exception("Remote agent refused credentials.");
            }
            catch (CommunicationFaultedException)
            {
                windowsProxies.Remove(id);
                throw new Exception("Communication with agent faulted.");
            }

        }
    }
}
