using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Backup.CommandsAttributes;
using BackupDatabase;
using BackupDatabase.Models;

namespace Backup.Commands
{
    public class Servers : ICommand
    {
        private readonly IMetaDBAccess metaDB;
        private readonly TextWriter outStream;

        public Servers(IMetaDBAccess metaDB, TextWriter outStream)
        {
            this.metaDB = metaDB;
            this.outStream = outStream;
        }

        [Action(name: "list")]
        public async Task Default()
        {
            var servers = await metaDB.GetServers();
            foreach (var server in servers)
            {
                await outStream.WriteLineAsync($"{server.Id.ToString()} - {server.Name} - {server.Type}");
            }
        }

        [Action(name: "details")]
        public async Task Details(Guid serverId)
        {
            var servertype = await metaDB.GetServerType(serverId);
            switch (servertype)
            {
                case ServerType.Windows:
                    await PrintWindowsServer(await metaDB.GetWindowsServer(serverId));
                    break;
                case ServerType.VMware:
                    await PrintVMwareServer(await metaDB.GetVMWareServer(serverId));
                    break;
            }

        }

        [Action(name: "add")]
        public async Task Add(ServerType type, string name, string ip, string username, string password)
        {
            DBServer server;
            switch (type)
            {
                case ServerType.Windows:
                    {
                        server = new DBWindowsServer
                        {
                            Name = name,
                            Ip = ip,
                            Username = username,
                            Password = password
                        };
                    }
                    break;
                case ServerType.VMware:
                    {
                        server = new DBVMwareServer
                        {
                            Name = name,
                            Ip = ip,
                            Username = username,
                            Password = password
                        };
                    }
                    break;
                default:
                    throw new ArgumentException($"Server type {type} unexpected!");
            }
            await metaDB.AddServer(server);
        }

        public async Task PrintUsage()
        {
            await outStream.WriteLineAsync("Usage: ...");
        }

        private async Task PrintWindowsServer(DBWindowsServer server)
        {
            await outStream.WriteLineAsync("Windows Server");
            await outStream.WriteLineAsync($"Id:       {server.Id}");
            await outStream.WriteLineAsync($"Name:     {server.Name}");
            await outStream.WriteLineAsync($"Ip:       {server.Ip}");
            await outStream.WriteLineAsync($"Username: {server.Username}");
        }

        private async Task PrintVMwareServer(DBVMwareServer server)
        {
            await outStream.WriteLineAsync("VMWare Server");
            await outStream.WriteLineAsync($"Id:       {server.Id}");
            await outStream.WriteLineAsync($"Name:     {server.Name}");
            await outStream.WriteLineAsync($"vCenter:  {server.Ip}");
            await outStream.WriteLineAsync($"Username: {server.Username}");
        }

    }
}
