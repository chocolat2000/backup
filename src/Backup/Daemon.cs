using Backup.Runners;
using BackupDatabase;
using BackupDatabase.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Backup
{
    public class Daemon
    {
        private static IObservable<long> observable;
        private static bool started = false;
        private static CancellationTokenSource ctsource = new CancellationTokenSource();
        private static IMetaDBAccess database;

        public static void Start(IMetaDBAccess database)
        {
            if (!started)
            {
                started = true;
                observable = Observable.Interval(TimeSpan.FromSeconds(2));
                Daemon.database = database;
                observable.Subscribe(_ => { WakeUp().GetAwaiter().GetResult(); }, ctsource.Token);
            }
        }

        public static void Stop()
        {
            ctsource.Cancel();
        }

        private static async Task WakeUp()
        {
            if (ctsource.IsCancellationRequested)
                return;

            try
            {
                var tasks = ((await database.GetNextEntries()).Select(calEntry =>
                {
                    Console.WriteLine();
                    Console.WriteLine("-------------------");
                    Console.WriteLine();
                    Console.WriteLine(calEntry.NextRun + " - " + calEntry.Id);

                    calEntry.UpdateNextRun();
                    return Task.Run(async () =>
                    {
                        if (ctsource.IsCancellationRequested)
                            return;

                        await database.AddCalendarEntry(calEntry);
                        switch (await database.GetServerType(calEntry.Server))
                        {
                            case ServerType.Windows:
                                {
                                    var backup = new AgentBackup(database);
                                    await backup.Run(calEntry.Server, calEntry.Items.ToArray());
                                }
                                break;
                            case ServerType.VMware:
                                {
                                    await Task.WhenAll(calEntry.Items.Select(vm =>
                                    {
                                        var backup = new VMwareBackup(database);
                                        return backup.Run(calEntry.Server, vm);
                                    }).ToArray());
                                }
                                break;
                        }

                    }, ctsource.Token);

                }));

                await Task.WhenAll(tasks.ToArray());
            }
            catch
            {
                Console.WriteLine("Database error !");
            }
        }
    }
}
