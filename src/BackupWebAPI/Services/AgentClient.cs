using BackupDatabase;
using BackupNetworkLibrary.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Threading.Tasks;
using AgentProxy;

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
            catch(Exception e)
            {
                return null;
            }
        }

        public async Task<FolderContent> GetContent(Guid id, string folder)
        {
            var proxy = GetWindowsProxy(id);
            try
            {
                return await proxy.GetContent(folder);
            }
            catch(Exception e)
            {
                return null;
            }
        }
    }
}
