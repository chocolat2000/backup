using Backup.Runners;
using BackupDatabase;
using BackupDatabase.Models;
using System;
using System.Collections.Concurrent;
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
                observable.Subscribe(_ =>  WakeUp(ctsource.Token).GetAwaiter().GetResult(), ctsource.Token);
            }
        }

        public static void Stop()
        {
            ctsource.Cancel();
        }

        private static async Task WakeUp(CancellationToken ctoken)
        {
            ctoken.ThrowIfCancellationRequested();

            try
            {
                var tasks = ((await database.GetNextCalendarEntries()).Select(calEntry =>
                {
                    Console.WriteLine();
                    Console.WriteLine("-------------------");
                    Console.WriteLine();
                    Console.WriteLine(calEntry.NextRun + " - " + calEntry.Id);

                    calEntry.UpdateNextRun();
                    return Task.Run(async () =>
                    {
                        ctoken.ThrowIfCancellationRequested();

                        await database.AddCalendarEntry(calEntry);
                        switch (await database.GetServerType(calEntry.Server))
                        {
                            case ServerType.Windows:
                                {
                                    using (var backup = new AgentBackup(database))
                                    {
                                        await backup.Run(calEntry.Server, calEntry.Items.ToArray(), ctoken);
                                    }
                                }
                                break;
                            case ServerType.VMware:
                                {
                                    await Task.WhenAll(calEntry.Items.Select(vm =>
                                    {
                                        var backup = new VMwareBackup(database);
                                        return backup.Run(calEntry.Server, vm, ctoken);
                                    }).ToArray());
                                }
                                break;
                        }

                    }, ctoken);

                }));

                await Task.WhenAll(tasks.ToArray());
            }
            catch(Exception e)
            {
                Console.WriteLine($"Database error: {e.Message}");
            }
        }
    }
}
