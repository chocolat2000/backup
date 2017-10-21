using Backup.Services;
using BackupDatabase;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Backup.Runners
{
    public class List
    {
        private IMetaDBAccess dataBase;

        public List(IMetaDBAccess dataBase)
        {
            this.dataBase = dataBase;
        }

        public async Task PrintBackups(TextWriter outStream)
        {
            var backups = await dataBase.GetBackups();
            var servers = (await dataBase.GetServers()).ToList();

            foreach(var backup in backups)
            {
                var size = await dataBase.BackupSize(backup.Id);
                await outStream.WriteLineAsync($"{backup.Id.ToString()} - {backup.StartDate.ToLocalTime()} - {servers.First(server => server.Id == backup.Server).Name} - {size} bytes");
            }
        }

        public async Task PrintFiles(Guid backup, TextWriter outStream)
        {
            var files = await dataBase.GetFiles(backup);
            foreach (var file in files)
            {
                await outStream.WriteLineAsync($"{file.Id.ToString()} - {file.Name} - {file.Length} bytes");
            }
        }

        public async Task PrintFolders(Guid backup, TextWriter outStream)
        {
            var folders = await dataBase.GetFolders(backup);
            foreach (var folder in folders)
            {
                await outStream.WriteLineAsync(folder.Name);
            }
        }

        public async Task PrintServers(TextWriter outStream)
        {
            var servers = await dataBase.GetServers();
            foreach (var server in servers)
            {
                await outStream.WriteLineAsync($"{server.Id.ToString()} - {server.Name} - {server.Type}");
            }

        }

        public async Task PrintCalendar(Guid server, TextWriter outStream)
        {
            var entries = await dataBase.GetServerCalendar(server);
            foreach (var entry in entries)
            {
                await outStream.WriteLineAsync($"{entry.Id.ToString()} - {entry.Periodicity} - {entry.NextRun}");
            }
        }

    }
}
