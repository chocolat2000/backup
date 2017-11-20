using System;
using BackupDatabase;
using BackupDatabase.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Backup.Services
{
    public class BlocksManager
    {
        private static IDBHashes hashesDB = new BackupDatabase.Cassandra.CassandraHashesDB("127.0.0.1");
        private static IDataDBAccess dataDB = new BackupDatabase.FileSystem.FileSystemDB(@"c:\backup\data");
        private static Murmur3 murmur = new Murmur3();
        private static SemaphoreSlim restoreConcurrent = new SemaphoreSlim(10,10);
        private static SemaphoreSlim addConcurrent = new SemaphoreSlim(1, 1);
        //private static ConcurrentExclusiveSchedulerPair scheduler = new ConcurrentExclusiveSchedulerPair();

        public static int dbBlocks = 0;

        public static async Task ScheduleReader(Func<Task> action)
        {
            Exception exception = null;

            await restoreConcurrent.WaitAsync();
            try
            {
                await action.Invoke();
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                restoreConcurrent.Release();
                if (exception != null)
                {
                    throw exception;
                }
            }
        }

        public static async Task<TResult> ScheduleReader<TResult>(Func<Task<TResult>> action)
        {
            TResult result = default(TResult);
            Exception exception = null;

            await restoreConcurrent.WaitAsync();
            try
            {
                result = await action.Invoke();
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                restoreConcurrent.Release();
                if (exception != null)
                {
                    throw exception;
                }
            }

            return result;
        }


        public static async Task ScheduleWriter(Func<Task> action)
        {
            Exception exception = null;

            await Task.WhenAll(new Task[]
            {
                restoreConcurrent.WaitAsync(),
                restoreConcurrent.WaitAsync(),
                restoreConcurrent.WaitAsync(),
                restoreConcurrent.WaitAsync(),
                restoreConcurrent.WaitAsync(),
                restoreConcurrent.WaitAsync(),
                restoreConcurrent.WaitAsync(),
                restoreConcurrent.WaitAsync(),
                restoreConcurrent.WaitAsync(),
                restoreConcurrent.WaitAsync(),
                addConcurrent.WaitAsync()
            });

            try
            {
                await action.Invoke();
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                restoreConcurrent.Release(10);
                if (exception != null)
                {
                    throw exception;
                }
            }

        }

        public static async Task<TResult> ScheduleWriter<TResult>(Func<Task<TResult>> action)
        {
            TResult result = default(TResult);
            Exception exception = null;

            await Task.WhenAll(new Task[]
            {
                restoreConcurrent.WaitAsync(),
                restoreConcurrent.WaitAsync(),
                restoreConcurrent.WaitAsync(),
                restoreConcurrent.WaitAsync(),
                restoreConcurrent.WaitAsync(),
                restoreConcurrent.WaitAsync(),
                restoreConcurrent.WaitAsync(),
                restoreConcurrent.WaitAsync(),
                restoreConcurrent.WaitAsync(),
                restoreConcurrent.WaitAsync(),
            });

            try
            {
                result = await action.Invoke();
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                restoreConcurrent.Release(10);
                addConcurrent.Release(1);
                if (exception != null)
                {
                    throw exception;
                }
            }

            return result;
        }

        public static async Task<Guid> AddBlockToDB(byte[] block, int length = -1)
        {
            Exception exception = null;
            var blockGuid = Guid.Empty;

            try
            {
                if (length == -1)
                {
                    length = block.Length;
                }

                var blockhash = new Guid(murmur.ComputeHash(block, 0, length));

                await addConcurrent.WaitAsync();

                var dbBlocks = await hashesDB.GetBlocksFromHash(blockhash);

                foreach (var blockID in dbBlocks)
                {
                    if (Equl(block, await dataDB.ReadBlock(blockID), length))
                    {
                        blockGuid = blockID;
                        continue;
                    }
                }

                if (blockGuid == Guid.Empty)
                {
                    BlocksManager.dbBlocks++;
                    blockGuid = Guid.NewGuid();
                    await dataDB.WriteBlock(blockGuid, block, length);
                }
                await hashesDB.AddHash(blockhash, blockGuid);
            }
            catch (Exception e)
            {
                exception = e;
            }
            finally
            {
                addConcurrent.Release();
                if (exception != null)
                {
                    throw exception;
                }
            }

            return blockGuid;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Equl(byte[] a, byte[] b, int length = -1)
        {
            var lgt = length;
            if (length == -1)
            {
                if (a.Length != b.Length)
                    return false;
                lgt = a.Length;
            }
            else
            {
                if (a.Length < length || b.Length < length)
                    return false;
            }

            var equal = true;
            Parallel.For(0, lgt, (i, s) =>
            {
                if (a[i] != b[i])
                {
                    equal = false;
                    s.Stop();
                }
            });
            return equal;
        }
        
        /*
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Equl(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;

            for (int i = 0; i < a.Length; i++)
                if (a[i] != b[i])
                    return false;

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool Equl(byte[] a, byte[] b, int length)
        {
            if (a.Length < length || b.Length < length)
                return false;

            for (int i = 0; i < length; i++)
                if (a[i] != b[i])
                    return false;

            return true;
        }
        */

    }
}
