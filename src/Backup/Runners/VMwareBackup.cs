using Backup.Services;
using BackupDatabase;
using BackupDatabase.Models;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Backup.Services.VDDK;
using System.Xml.Serialization;
using System.IO;
using System.IO.Compression;
using Vim25Proxy;

namespace Backup.Runners
{
    public class VMwareBackup : BackupRunner
    {
        private IMetaDBAccess metaDB;

        private BlockSplitter hasher;

        private Proxy vim25Proxy = null;

        private DBVMwareServer _server = null;
        private DBVMwareServer Server
        {
            get { return _server; }
            set
            {
                if (value?.Id != _server?.Id)
                {
                    _server = value;
                    if(vim25Proxy != null)
                    {
                        vim25Proxy.Dispose();
                        vim25Proxy = null;
                    }
                }
            }
        }

        public VMwareBackup(IMetaDBAccess metaDB)
        {
            this.metaDB = metaDB;
            hasher = new BlockSplitter();
        }

        public async Task Run(Guid serverId, string vmMoref = null, CancellationToken ctoken = default)
        {
            if (backup != null)
                return;

            this.ctoken = ctoken;

            Server = await metaDB.GetVMWareServer(serverId, true);

            backup = new DBBackup { Server = serverId, StartDate = DateTime.UtcNow, Status = Status.Running, Log = new List<string>() };
            backup.Id = await metaDB.AddBackup(backup);

            var vmBackup = new DBVMwareVM { Backup = backup.Id, Moref = vmMoref, StartDate = DateTime.UtcNow, Valid = false, Server = Server.Id };
            vmBackup.Id = await metaDB.AddVMwareVM(vmBackup);

            var snapRef = string.Empty;

            try
            {

                vim25Proxy = new Proxy(Server.Ip);
                await vim25Proxy.Login(Server.Username, Server.Password);
                if (vmMoref == null)
                {
                    int selected = -1;
                    var vmList = (await vim25Proxy.GetVMs()).OrderBy(kv => kv.Value).ToArray();
                    do
                    {
                        var i = 1;
                        foreach (var kv in vmList)
                        {
                            Console.WriteLine("{0}\t{1}", i++, kv.Value);
                        }
                        Console.WriteLine("Choose a VM: ");
                        if (!int.TryParse(Console.ReadLine(), out selected) || selected < 1 || selected > vmList.Length)
                        {
                            selected = -1;
                        }
                    } while (selected < 1);
                    vmMoref = vmList[selected - 1].Key;
                }

                CheckCancelStatus();

                // Retrieve backup vm info, usefull for backup type
                var vmPowerState = await vim25Proxy.GetVMPowerState(vmMoref);

                var (changeTrackingEnabled, changeTrackingSupported) = await vim25Proxy.GetCBTState(vmMoref);

                // Check and enable CBT
                if (!changeTrackingEnabled && changeTrackingSupported)
                {
                    await vim25Proxy.ConfigureForCBT(vmMoref);
                }

                // Create backup snapshot
                snapRef = await vim25Proxy.CreateSnapshot(
                        vmMoref, "BerBackup", $"Backup started at {DateTime.Now}",
                        false, vmPowerState == VirtualMachinePowerState.poweredOn);

                // Retrieve full vm config from Snapshot
                var vmConfig = await vim25Proxy.GetVMConfigFromSnapshot(snapRef);

                vmBackup.Name = vim25Proxy.GetVMConfigParameter(vmConfig, "name") as string;

                //vmBackup.Config = vim25Proxy.SerializeVMConfig(vmConfig);

                vmBackup.Id = await metaDB.AddVMwareVM(vmBackup);

                var previousVMBackup = changeTrackingEnabled ? await metaDB.GetLatestVM(serverId, vmMoref) : null;

                // Initialize VDDK stuff
                Environment.SetEnvironmentVariable("PATH", @"C:\Users\bdoneux\source\repos\Backup\x64\Debug;D:\VMware-vix-disklib-6.5.2-6195444.x86_64\bin;%PATH%");
                var status = VixDiskLib.VixDiskLib_InitEx(6, 0, null, null, null, @"D:\VMware-vix-disklib-6.5.2-6195444.x86_64", null);

                var connectParams = new VixDiskLibConnectParams
                {
                    serverName = Server.Ip,
                    thumbPrint = Server.ThumbPrint,
                    credType = VixDiskLibCredType.VIXDISKLIB_CRED_UID,
                    port = 0,
                    nfcHostPort = 0,
                    vmxSpec = string.Format("moref={0}", vmMoref)
                };

                connectParams.creds.uid.userName = Server.Username;
                connectParams.creds.uid.password = Server.Password;

                var vixConnHandle = IntPtr.Zero;
                var vixDiskHandle = IntPtr.Zero;

                status = VixDiskLib.VixDiskLib_ConnectEx(connectParams, (char)VixDiskLib.TRUE, snapRef, null, out vixConnHandle);

                var blocksReadBatch = 2048UL;
                var vddkReadBuffer = new byte[blocksReadBatch * 512];

                foreach (var disk in vim25Proxy.GetDisksFromConfig(vmConfig))
                {

                    var diskInfo = vim25Proxy.GetDiskInfo(disk);

                    status = VixDiskLib.VixDiskLib_PrepareForAccess(connectParams, "BackupSoft");
                    status = VixDiskLib.VixDiskLib_Open(vixConnHandle, diskInfo.Path, (uint)VixDiskLib.VIXDISKLIB_FLAG_OPEN_READ_ONLY, out vixDiskHandle);

                    status = VixDiskLib.VixDiskLib_GetMetadataKeys(vixDiskHandle, null, 1, out uint metaLength);
                    var diskMeta = new byte[metaLength];
                    status = VixDiskLib.VixDiskLib_GetMetadataKeys(vixDiskHandle, diskMeta, metaLength, out metaLength);

                    var dbDisk = new DBVMDisk { VM = vmBackup.Id, Path = diskInfo.Path, Key = diskInfo.Key, ChangeId = diskInfo.ChangeId, Metadata = diskMeta, Length = diskInfo.Capacity, Valid = false };
                    dbDisk.Id = await metaDB.AddVMDisk(dbDisk);

                    Console.WriteLine(DateTime.Now);
                    Console.WriteLine($"Backup VM disk : {diskInfo.Path}");

                    var previousChangeId = "*";
                    var previousDisk = previousVMBackup != null ? await metaDB.GetVMDisk(previousVMBackup.Id, diskInfo.Key) : null;
                    if (previousDisk != null)
                    {
                        previousChangeId = previousDisk.ChangeId;
                        await metaDB.CopyVMDiskBlocks(previousDisk.VM, dbDisk.Id);
                    }

                    long bytestart = 0;
                    long bytelength = 0;
                    ulong iterations;
                    ulong startsector;
                    ulong lastSectors;

                    BlocksManager.dbBlocks = 0;
                    IList<byte[]> blocks;
                    var totalBlocks = 0;

                    int alignoffset = 0;

                    while (bytestart < diskInfo.Capacity)
                    {
                        //Console.WriteLine("ChangeInfo: start: {0}, length: {1}", diskChangeInfo.startOffset, diskChangeInfo.length);

                        foreach (var (start, length) in await vim25Proxy.GetDiskChangedAreas(vmMoref, snapRef, diskInfo.Key, bytestart, previousChangeId))
                        {
                            if (bytestart < start)
                            {
                                bytestart = start;
                            }

                            var _bytestart = await metaDB.GetPreviousVMDiskOffset(dbDisk.Id, bytestart);
                            if (_bytestart > 0)
                            {
                                bytestart = _bytestart;
                            }

                            var _byteend = await metaDB.GetNextVMDiskOffset(dbDisk.VM, start + length);
                            if (_byteend > 0)
                            {
                                bytelength = _byteend - bytestart;
                            }
                            else
                            {
                                bytelength = start + length - bytestart;
                            }


                            iterations = (Convert.ToUInt64(bytelength) / 512) / blocksReadBatch;
                            lastSectors = (Convert.ToUInt64(bytelength) / 512) % blocksReadBatch;

                            startsector = Convert.ToUInt64(bytestart) / 512;
                            alignoffset = Convert.ToInt32(bytestart % 512);
                            var startNotAligned = alignoffset > 0;

                            hasher.Initialize();

                            for (var pos = 0UL; pos < iterations; pos++)
                            {
                                status = VixDiskLib.VixDiskLib_Read(vixDiskHandle, startsector, blocksReadBatch, vddkReadBuffer);
                                startsector += blocksReadBatch;
                                if (startNotAligned)
                                {
                                    blocks = hasher.NextBlock(vddkReadBuffer, alignoffset);
                                    startNotAligned = false;
                                }
                                else
                                {
                                    blocks = hasher.NextBlock(vddkReadBuffer, 0);
                                }
                                foreach (var block in blocks)
                                {
                                    var blockGuid = await BlocksManager.AddBlockToDB(block);
                                    await metaDB.AddVMDiskBlock(new DBVMDiskBlock { Block = blockGuid, VMDisk = dbDisk.Id, Offset = bytestart });
                                    bytestart += block.Length;
                                    totalBlocks++;
                                }
                            }

                            if (lastSectors > 0UL)
                            {
                                status = VixDiskLib.VixDiskLib_Read(vixDiskHandle, startsector, lastSectors, vddkReadBuffer);
                                startsector += lastSectors;
                                if (startNotAligned)
                                {
                                    blocks = hasher.NextBlock(vddkReadBuffer, alignoffset, 512 * Convert.ToInt32(lastSectors));
                                    startNotAligned = false;
                                }
                                else
                                {
                                    blocks = hasher.NextBlock(vddkReadBuffer, 0, 512 * Convert.ToInt32(lastSectors));
                                }
                                foreach (var block in blocks)
                                {
                                    var blockGuid = await BlocksManager.AddBlockToDB(block);
                                    await metaDB.AddVMDiskBlock(new DBVMDiskBlock { Block = blockGuid, VMDisk = dbDisk.Id, Offset = bytestart });
                                    bytestart += block.Length;
                                    totalBlocks++;
                                }
                            }

                            if (hasher.HasRemainingBytes)
                            {
                                var diskSizeInSectors = Convert.ToUInt64(diskInfo.Capacity) / 512;
                                while (startsector < diskSizeInSectors)
                                {
                                    var readSize = Math.Min(blocksReadBatch, diskSizeInSectors - startsector);
                                    status = VixDiskLib.VixDiskLib_Read(vixDiskHandle, startsector, readSize, vddkReadBuffer);
                                    startsector += readSize;
                                    if (startNotAligned)
                                    {
                                        blocks = hasher.NextBlock(vddkReadBuffer, alignoffset, Convert.ToInt32(readSize) * 512);
                                        startNotAligned = false;
                                    }
                                    else
                                    {
                                        blocks = hasher.NextBlock(vddkReadBuffer, 0, Convert.ToInt32(readSize) * 512);
                                    }
                                    if (blocks.Count > 0)
                                    {
                                        var block = blocks[0];
                                        var blockGuid = await BlocksManager.AddBlockToDB(block);
                                        await metaDB.AddVMDiskBlock(new DBVMDiskBlock { Block = blockGuid, VMDisk = dbDisk.Id, Offset = bytestart });
                                        bytestart += block.Length;
                                        totalBlocks++;
                                        break;
                                    }
                                }

                                if (startsector >= diskSizeInSectors && hasher.HasRemainingBytes)
                                {
                                    var block = hasher.RemainingBytes();
                                    var blockGuid = await BlocksManager.AddBlockToDB(block);
                                    await metaDB.AddVMDiskBlock(new DBVMDiskBlock { Block = blockGuid, VMDisk = dbDisk.Id, Offset = bytestart });
                                    bytestart += block.Length;
                                    totalBlocks++;
                                }
                            }

                            Console.WriteLine(DateTime.Now);

                            Console.WriteLine("Total blocks : " + totalBlocks);
                            Console.WriteLine("DB blocks : " + BlocksManager.dbBlocks);

                        }

                    }

                    Console.WriteLine(DateTime.Now);

                    Console.WriteLine("Total blocks : " + totalBlocks);
                    Console.WriteLine("DB blocks : " + BlocksManager.dbBlocks);

                    dbDisk.Valid = true;
                    await metaDB.AddVMDisk(dbDisk);

                    status = VixDiskLib.VixDiskLib_Close(vixDiskHandle);

                    dbDisk.Valid = true;
                    await metaDB.AddVMDisk(dbDisk);
                }


                status = VixDiskLib.VixDiskLib_Disconnect(vixConnHandle);
                VixDiskLib.VixDiskLib_Exit();

                vmBackup.EndDate = DateTime.UtcNow;
                vmBackup.Valid = true;
                await metaDB.AddVMwareVM(vmBackup);

            }
            catch (Exception e)
            {
                Console.WriteLine($"{backup.Id} - Backup failed: {e.Message}");
                backup.AppendLog(e.Message);
                backup.Status = Status.Failed;
            }
            finally
            {
                if (vim25Proxy.IsConnected)
                {
                    if (snapRef != string.Empty)
                        await vim25Proxy.RemoveSnapshot(snapRef, false, true);
                    await vim25Proxy.Logout();
                }
            }
            vmBackup.EndDate = DateTime.UtcNow;
            await metaDB.AddVMwareVM(vmBackup);

            backup.EndDate = DateTime.UtcNow;
            await metaDB.AddBackup(backup);

        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    vim25Proxy?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~AgentBackup() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public override void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion


    }
}
