using BackupDatabase;
using BackupDatabase.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backup.Runners
{
    public class ManageServers
    {
        private IMetaDBAccess dataBase;

        public ManageServers(IMetaDBAccess dataBase)
        {
            this.dataBase = dataBase;
        }

        public async Task AddWindowsServer(string serverName, string ip, int port)
        {
            await dataBase.AddServer(new DBWindowsServer { Name = serverName, Ip = ip, Port = port, Type = ServerType.Windows });
        }

        public async Task AddVMwareServer(string serverName, string ip, string username, string password)
        {
            await dataBase.AddServer(new DBVMwareServer { Name = serverName, Ip = ip, Username = username, Password = password, Type = ServerType.VMware });
        }

        public async Task AddCalendarEntry(Guid server, DateTime firstRun, Periodicity periodicity, IEnumerable<string> items)
        {
            var entry = new DBCalendarEntry { Server = server, FirstRun = firstRun, Periodicity = periodicity, Values = new int[] { }, Items = items };
            entry.UpdateNextRun();
            await dataBase.AddCalendarEntry(entry);
        }


    }
}
