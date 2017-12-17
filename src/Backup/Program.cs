using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections;
using Backup.Runners;
using BackupDatabase.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace Backup
{
    public class Program
    {
        private static IConfiguration Configuration { get; set; }

        public static void Main(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json");

            Configuration = builder.Build();

            Run(args).GetAwaiter().GetResult();
        }

        private static async Task Run(string[] args)
        {
            using (var metaDB = new BackupDatabase.Cassandra.CassandraMetaDB(Configuration["Database:CassandraMetaIP"]) { PasswordsKey = Encoding.UTF8.GetBytes(Configuration["Encryption:PasswordsKey"]) })
            using (var usersDB = new BackupDatabase.Cassandra.CassandraUsersDB(Configuration["Database:CassandraMetaIP"]))
            {
                Console.Write("> ");
                var command = (Console.ReadLine() ?? "").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                while (command?.Length == 0 || command[0] != "exit")
                {
                    if (command.Length > 0)
                    {
                        switch (command[0])
                        {
                            case "daemon":
                                {
                                    Daemon.Start(metaDB);
                                }
                                break;
                            case "backup":
                                {
                                    switch (command[1])
                                    {
                                        case "agent":
                                            {
                                                using (var backup = new AgentBackup(metaDB))
                                                    await backup.Run(new Guid(command[2]), command.Skip(3).ToArray(), new CancellationTokenSource().Token);
                                            }
                                            break;
                                        case "vmware":
                                            {
                                                var backup = new VMwareBackup(metaDB);
                                                await backup.Run(new Guid(command[2]), command.Length > 3 ? command[3] : null);
                                            }
                                            break;
                                        default:
                                            {
                                                PrintUsage();
                                            }
                                            break;
                                    }

                                }
                                break;
                            case "restore":
                                {
                                    switch (command[1])
                                    {
                                        case "file":
                                            {
                                                if (command.Length != 4)
                                                {
                                                    PrintUsage();
                                                }
                                                else
                                                {
                                                    var restore = new Restore(metaDB);
                                                    await restore.RestoreFile(new Guid(command[2]), command[3]);
                                                }
                                            }
                                            break;
                                        default:
                                            {
                                                PrintUsage();
                                            }
                                            break;
                                    }
                                }
                                break;
                            case "list":
                                {
                                    switch (command[1])
                                    {
                                        case "servers":
                                            {
                                                var list = new List(metaDB);
                                                await list.PrintServers(Console.Out);
                                            }
                                            break;
                                        case "backups":
                                            {
                                                var list = new List(metaDB);
                                                await list.PrintBackups(Console.Out);
                                            }
                                            break;
                                        case "files":
                                            {
                                                if (command.Length != 3)
                                                {
                                                    PrintUsage();
                                                }
                                                else
                                                {
                                                    var list = new List(metaDB);
                                                    await list.PrintFiles(new Guid(command[2]), Console.Out);
                                                }
                                            }
                                            break;
                                        case "folders":
                                            {
                                                if (command.Length != 3)
                                                {
                                                    PrintUsage();
                                                }
                                                else
                                                {
                                                    var list = new List(metaDB);
                                                    await list.PrintFolders(new Guid(command[2]), Console.Out);
                                                }
                                            }
                                            break;
                                        case "calendar":
                                            {
                                                if (command.Length != 3)
                                                {
                                                    PrintUsage();
                                                }
                                                else
                                                {
                                                    var list = new List(metaDB);
                                                    await list.PrintCalendar(new Guid(command[2]), Console.Out);
                                                }
                                            }
                                            break;

                                        default:
                                            {
                                                PrintUsage();
                                            }
                                            break;
                                    }
                                }
                                break;
                            case "servers":
                                {
                                    switch (command[1])
                                    {
                                        case "add":
                                            {
                                                if (command.Length < 6)
                                                {
                                                    PrintUsage();
                                                }
                                                else
                                                {
                                                    switch (command[2])
                                                    {
                                                        case "windows":
                                                            {
                                                                var serversManager = new ManageServers(metaDB);
                                                                await serversManager.AddWindowsServer(command[3], command[4], int.Parse(command[5], System.Globalization.NumberStyles.Integer));
                                                            }
                                                            break;
                                                        case "vmware":
                                                            {
                                                                if (command.Length < 7)
                                                                {
                                                                    PrintUsage();
                                                                }
                                                                else
                                                                {
                                                                    var serversManager = new ManageServers(metaDB);
                                                                    await serversManager.AddVMwareServer(command[3], command[4], command[5], command[6]);
                                                                }
                                                            }
                                                            break;
                                                    }
                                                }
                                            }
                                            break;

                                        case "jobs":
                                            {
                                                var serversManager = new ManageServers(metaDB);
                                                Enum.TryParse(command[4], out Periodicity periodicity);
                                                await serversManager.AddCalendarEntry(new Guid(command[2]), Convert.ToDateTime(command[3]), periodicity, command.Skip(5));
                                            }
                                            break;

                                    }
                                }
                                break;
                            case "users":
                                {
                                    if (command.Length < 4)
                                    {
                                        PrintUsage();
                                    }
                                    else
                                    {
                                        switch (command[1])
                                        {
                                            case "add":
                                                {
                                                    var password = "";
                                                    ConsoleKeyInfo key;
                                                    Console.Write("Choose password: ");
                                                    do
                                                    {
                                                        key = Console.ReadKey(true);
                                                        if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                                                        {
                                                            password = password.Substring(0, (password.Length - 1));
                                                        }
                                                        else
                                                        {
                                                            if (key.Key != ConsoleKey.Enter)
                                                            {
                                                                password += key.KeyChar;
                                                            }
                                                        }

                                                    } while (key.Key != ConsoleKey.Enter);
                                                    Console.WriteLine();
                                                    await usersDB.AddUser(command[3], password, new string[] { command[2] });
                                                }
                                                break;
                                        }
                                    }

                                }
                                break;
                            default:
                                {
                                    Console.WriteLine("Unknown command");
                                }
                                break;
                        }
                    }
                    Console.Write("> ");
                    command = (Console.ReadLine() ?? "").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                }
            }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Please enter file name");
        }

    }
}
