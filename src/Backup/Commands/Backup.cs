using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Backup.CommandsAttributes;
using BackupDatabase;
using BackupDatabase.Models;
using AgentProxy;

namespace Backup.Commands
{
    [Command(name: "backup")]
    public class Backup : ICommand
    {
        private readonly IMetaDBAccess metaDB;
        private readonly TextWriter outStream;
        private readonly TextReader inStream;

        public Backup(IMetaDBAccess metaDB, TextWriter outStream, TextReader inStream)
        {
            this.metaDB = metaDB;
            this.outStream = outStream;
            this.inStream = inStream;
        }

        public async Task Default()
        {
            await PrintUsage();
        }

        [Action(name: "list")]
        public async Task List()
        {
            await PrintBackups(await metaDB.GetBackups());
        }

        [Action(name: "cancel")]
        public async Task Cancel(Guid id)
        {
            await metaDB.CancelBackup(id);
            await outStream.WriteLineAsync($"Backup {{{id}}} canceled");
        }

        [Action(name: "wizard")]
        public async Task Wizard()
        {

            await outStream.WriteLineAsync(
                "Welcome to th backup Wizard\n" +
                "First, select a server to backup");

            var server = AskForServer();

        }

        [Action(name: "agent")]
        public void Agent(Guid serverId, params string[] items)
        {
            Console.WriteLine("Backup agent: {0}", string.Join(", ", items));
        }

        [Action(name: "vmware")]
        public void VMware(Guid serverId, params string[] vmMorefs)
        {
            Console.WriteLine("Backup VMware: {0}", string.Join(", ", vmMorefs));
        }

        public async Task PrintUsage()
        {
            await outStream.WriteLineAsync("Try somesing else ...");
        }

        private async Task PrintBackups(IEnumerable<DBBackup> backups)
        {
            await outStream.WriteLineAsync("Backups");
            foreach (var backup in backups)
            {
                await outStream.WriteLineAsync(
                    $"{backup.Id}  {backup.Server}  {backup.StartDate}  {backup.EndDate}  {backup.Status}");
            }
        }

        private async Task<DBServer> AskForServer()
        {
            var servers = (await metaDB.GetServers()).ToArray();
            var i = 0;
            foreach (var server in servers)
            {
                await outStream.WriteLineAsync(
                    $"{i++}: {server.Name} - {server.Type}");
            }

            DBServer chosenServer = null;
            do
            {
                await outStream.WriteAsync("Choose a number > ");
                try
                {
                    var rowId = UInt32.Parse(await inStream.ReadLineAsync());
                    chosenServer = servers[rowId];
                }
                catch
                {
                    chosenServer = null;
                }
            } while (chosenServer == null);

            return chosenServer;
        }

        private async Task<string[]> AskForItemsAgent(Guid serverId)
        {
            var server = await metaDB.GetWindowsServer(serverId, true);
            if (server == null) return null;

            var proxy = new WindowsProxy(server.Ip, server.Username, server.Password);
            var drives = await proxy.GetDrives();

            var items = new List<string>();

            await outStream.WriteLineAsync("Choose something to backup");

            var i = 0;
            foreach(var drive in drives)
            {
                await outStream.WriteLineAsync(
                    $"{i++} / {drive}");
            }
            await outStream.WriteAsync("number to add -or- g number to go deeper -or- b to go back > ");
            var rawInput = await inStream.ReadLineAsync() ?? "";

            var stringArray = rawInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return items.ToArray();
        }
    }
}
