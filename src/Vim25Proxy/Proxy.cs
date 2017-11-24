using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Collections.Generic;
using System.ServiceModel;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Vim25Api;
using Vim25Proxy.Models;

namespace Vim25Proxy
{
    public class Proxy : IDisposable
    {

        private UserSession session = null;
        private ServiceContent serviceContent = null;
        private readonly string server;

        private VimPortTypeClient vim25Client = null;
        private VimPortTypeClient Vim25Client
        {
            get
            {
                if (vim25Client == null)
                {
                    var vim25Binding = new BasicHttpsBinding { MaxReceivedMessageSize = int.MaxValue, MaxBufferPoolSize = int.MaxValue, MaxBufferSize = int.MaxValue, AllowCookies = true };
                    vim25Client = new VimPortTypeClient(vim25Binding, new EndpointAddress($"https://{server}/sdk"));
                }
                return vim25Client;
            }
        }

        public bool IsConnected
        {
            get
            {
                return session != null && Vim25Client.State == CommunicationState.Opened;
            }
        }

        static Proxy()
        {
            ServicePointManager.ServerCertificateValidationCallback +=
                new RemoteCertificateValidationCallback((sender, certificate, chain, errors) => { return true; });
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        public Proxy(string server)
        {
            if (string.IsNullOrWhiteSpace(server))
                throw new ArgumentException("Argument cannot be null or empty", nameof(server));

            this.server = server;

        }

        public async Task Login(string username, string password, string locale = null)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentException("Argument cannot be null or empty", nameof(username));

            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentException("Argument cannot be null or empty", nameof(password));

            var serviceRef = new ManagedObjectReference
            {
                type = "ServiceInstance",
                Value = "ServiceInstance"
            };

            serviceContent = await Vim25Client.RetrieveServiceContentAsync(serviceRef);
            session = await Vim25Client.LoginAsync(serviceContent.sessionManager, username, password, locale);
        }

        public async Task Logout()
        {
            if (session == null)
                throw new Exception("Not Logged");

            await Vim25Client.LogoutAsync(serviceContent.sessionManager);

            session = null;
        }

        public async Task<string> CreateSnapshot(string virtualMachineMoRef, string name, string description, bool memory, bool quiesce)
        {
            if (session == null)
                throw new Exception("Not Logged");

            var taskRef = await Vim25Client.CreateSnapshot_TaskAsync(new ManagedObjectReference { type = "VirtualMachine", Value = virtualMachineMoRef }, name, description, memory, quiesce);

            var taskInfo = await WaitForTaskEnd(taskRef);
            if (taskInfo.state != TaskInfoState.success)
                throw new Exception(taskInfo.error.localizedMessage);
            var snapRef = taskInfo.result as ManagedObjectReference;
            return snapRef.Value;
        }

        public async Task RemoveSnapshot(string snapshotMoRef, bool removeChildren, bool consolidate)
        {
            if (session == null)
                throw new Exception("Not Logged");

            var taskRef = await Vim25Client.RemoveSnapshot_TaskAsync(new ManagedObjectReference { type = "VirtualMachineSnapshot", Value = snapshotMoRef }, removeChildren, consolidate);

            var taskInfo = await WaitForTaskEnd(taskRef);
            if (taskInfo.state != TaskInfoState.success)
                throw new Exception(taskInfo.error.localizedMessage);

        }

        public async Task<object> GetVMConfigFromSnapshot(string snapshotMoRef)
        {
            if (session == null)
                throw new Exception("Not Logged");

            var propertySpecSnap = new PropertySpec
            {
                pathSet = new string[] { "config" },
                type = "VirtualMachineSnapshot"
            };
            var propertyFilterSpecSnap = new PropertyFilterSpec
            {
                propSet = new PropertySpec[] { propertySpecSnap },
                objectSet = new ObjectSpec[] { new ObjectSpec() { obj = new ManagedObjectReference { type = "VirtualMachineSnapshot", Value = snapshotMoRef } } }
            };
            var snapProps = await Vim25Client.RetrievePropertiesAsync(serviceContent.propertyCollector, new PropertyFilterSpec[] { propertyFilterSpecSnap });

            return snapProps.returnval[0].propSet.FirstOrDefault(p => p.name == "config")?.val as VirtualMachineConfigInfo;

        }

        public async Task CreateVM(string folderMoRef, object configSpec, string resourcePoolMoRef, string hostMoRef)
        {
            if (session == null)
                throw new Exception("Not Logged");

            var vmConfig = configSpec as VirtualMachineConfigSpec;
            if (vmConfig == null)
                throw new ArgumentException($"{nameof(configSpec)} must be of type VirtualMachineConfigSpec");

            var folder = new ManagedObjectReference { type = "Folder", Value = folderMoRef };
            var pool = new ManagedObjectReference { type = "ResourcePool", Value = resourcePoolMoRef };
            var host = new ManagedObjectReference { type = "Host", Value = hostMoRef };

            var createTask = await Vim25Client.CreateVM_TaskAsync(folder, vmConfig, pool, host);
            var taskInfo = await WaitForTaskEnd(createTask);
            if (taskInfo.state != TaskInfoState.success)
                throw new Exception(taskInfo.error.localizedMessage);

        }

        public byte[] SerializeVMConfig(object vmConfig)
        {
            var sourceConfig = vmConfig as VirtualMachineConfigInfo;
            if (sourceConfig == null)
                throw new ArgumentException($"{nameof(vmConfig)} must be of type VirtualMachineConfigInfo");

            var configSpec = new VirtualMachineConfigSpec
            {
                name = sourceConfig.name,
                version = sourceConfig.version,
                guestId = sourceConfig.guestId,
                alternateGuestName = sourceConfig.alternateGuestName,
                files = sourceConfig.files,
                tools = sourceConfig.tools,
                flags = sourceConfig.flags,
                consolePreferences = null,
                powerOpInfo = sourceConfig.defaultPowerOps,
                numCPUs = sourceConfig.hardware.numCPU,
                memoryMB = sourceConfig.hardware.memoryMB,
                cpuHotAddEnabled = sourceConfig.cpuHotAddEnabled,
                memoryHotAddEnabled = sourceConfig.memoryHotAddEnabled,
                numCoresPerSocket = sourceConfig.hardware.numCoresPerSocket,
                cpuFeatureMask = sourceConfig.cpuFeatureMask.Select(info => new VirtualMachineCpuIdInfoSpec { operation = ArrayUpdateOperation.add, info = info }).ToArray(),
                deviceChange = sourceConfig.hardware.device.Where(
                    device =>
                        !(
                        device is VirtualIDEController ||
                        device is VirtualPS2Controller ||
                        device is VirtualPCIController ||
                        device is VirtualSIOController ||
                        device is VirtualMachineVMCIDevice ||
                        device is VirtualKeyboard ||
                        device is VirtualPointingDevice
                        )
                    ).Select(
                    device => new VirtualDeviceConfigSpec
                    {
                        operation = VirtualDeviceConfigSpecOperation.add,
                        device = device,

                    }).ToArray(),
                cpuAllocation = sourceConfig.cpuAllocation,
                memoryAllocation = sourceConfig.memoryAllocation,
                cpuAffinity = null,
                memoryAffinity = null,
                networkShaper = null,
                bootOptions = null
            };

            byte[] result = null;
            using (var memStream = new MemoryStream())
            {
                using (var zippedStream = new DeflateStream(memStream, CompressionMode.Compress))
                using (var writeStream = new StreamWriter(zippedStream))
                {
                    writeStream.Write(JsonConvert.SerializeObject(configSpec));
                }
                result = memStream.ToArray();
            }

            return result;
        }

        public object DeSerializeVMConfig(byte[] serializedVirtualMachineConfig)
        {
            VirtualMachineConfigSpec virtualMachineConfigSpec;
            using (var memStream = new MemoryStream(serializedVirtualMachineConfig))
            {
                using (var zippedStream = new DeflateStream(memStream, CompressionMode.Decompress))
                using (var readStream = new StreamReader(zippedStream))
                {
                    virtualMachineConfigSpec = JsonConvert.DeserializeObject<VirtualMachineConfigSpec>(readStream.ReadToEnd());
                }
            }

            return virtualMachineConfigSpec;
        }

        public object GetVMConfigParameter(object vmConfig, string value)
        {
            if (vmConfig == null || !(vmConfig is VirtualMachineConfigInfo))
                throw new ArgumentNullException(nameof(vmConfig));

            if(string.IsNullOrEmpty(value))
                throw new ArgumentNullException(nameof(value));

            var property = vmConfig.GetType().GetProperty(value);
            if (property == null)
                throw new ArgumentException(nameof(value));

            return property.GetValue(vmConfig, null);
        }

        public IEnumerable<object> GetDisksFromConfig(object vmConfig)
        {
            if (vmConfig == null || !(vmConfig is VirtualMachineConfigInfo))
                throw new ArgumentNullException(nameof(vmConfig));

            return ((VirtualMachineConfigInfo)vmConfig).hardware.device.Where(device => device is VirtualDisk);
        }

        public DiskInfo GetDiskInfo(object disk)
        {
            if (disk == null || !(disk is VirtualDisk))
                throw new ArgumentNullException(nameof(disk));

            var _disk = disk as VirtualDisk;
            var vmdkBackingInfo = _disk.backing as VirtualDeviceFileBackingInfo;
            var newChangeId = "*";

            if (vmdkBackingInfo is VirtualDiskFlatVer2BackingInfo)
            {
                newChangeId = ((VirtualDiskFlatVer2BackingInfo)vmdkBackingInfo).changeId;
            }
            else if (vmdkBackingInfo is VirtualDiskSparseVer2BackingInfo)
            {
                newChangeId = ((VirtualDiskSparseVer2BackingInfo)vmdkBackingInfo).changeId;
            }
            else if (vmdkBackingInfo is VirtualDiskRawDiskMappingVer1BackingInfo)
            {
                newChangeId = ((VirtualDiskRawDiskMappingVer1BackingInfo)vmdkBackingInfo).changeId;
            }

            return new DiskInfo { Path = vmdkBackingInfo.fileName, ChangeId = newChangeId, Key = _disk.key, Capacity = _disk.capacityInBytes };
        }

        public async Task<IEnumerable<(long start, long length)>> GetDiskChangedAreas(string vmMoRef, string snapshotMoRef, int diskKey, long startOffset, string changeId = "*")
        {
            if (session == null)
                throw new Exception("Not Logged");

            var diskChangeInfo = await Vim25Client.QueryChangedDiskAreasAsync(
                    new ManagedObjectReference { type = "VirtualMachine", Value = vmMoRef },
                    new ManagedObjectReference { type = "VirtualMachineSnapshot", Value = snapshotMoRef },
                    diskKey, startOffset, changeId);

            if (diskChangeInfo.changedArea == null)
                return Enumerable.Empty<(long start, long length)>();

            return diskChangeInfo.changedArea.OrderBy(change => change.start).Select(change => (change.start, change.length));
        }

        public async Task<VirtualMachinePowerState> GetVMPowerState(string vmMoRef)
        {
            if (session == null)
                throw new Exception("Not Logged");

            var propertySpec = new PropertySpec
            {
                pathSet = new string[] { "runtime.powerState" },
                type = "VirtualMachine"
            };
            var propertyFilterSpec = new PropertyFilterSpec
            {
                propSet = new PropertySpec[] { propertySpec },
                objectSet = new ObjectSpec[] { new ObjectSpec() { obj = new ManagedObjectReference { type = "VirtualMachine", Value = vmMoRef } } }
            };

            var vmPowerStateReq = await Vim25Client.RetrievePropertiesAsync(serviceContent.propertyCollector, new PropertyFilterSpec[] { propertyFilterSpec });
            return (VirtualMachinePowerState)vmPowerStateReq.returnval[0].propSet.FirstOrDefault(p => p.name == "runtime.powerState")?.val;

        }

        public async Task<(bool, bool)> GetCBTState(string vmMoRef)
        {
            if (session == null)
                throw new Exception("Not Logged");

            var propertySpec = new PropertySpec
            {
                pathSet = new string[] { "config.changeTrackingEnabled", "capability.changeTrackingSupported" },
                type = "VirtualMachine"
            };
            var propertyFilterSpec = new PropertyFilterSpec
            {
                propSet = new PropertySpec[] { propertySpec },
                objectSet = new ObjectSpec[] { new ObjectSpec() { obj = new ManagedObjectReference { type = "VirtualMachine", Value = vmMoRef } } }
            };

            var vmPowerStateReq = await Vim25Client.RetrievePropertiesAsync(serviceContent.propertyCollector, new PropertyFilterSpec[] { propertyFilterSpec });
            var changeTrackingEnabled = (bool)vmPowerStateReq.returnval[0].propSet.FirstOrDefault(p => p.name == "config.changeTrackingEnabled")?.val;
            var changeTrackingSupported = (bool)vmPowerStateReq.returnval[0].propSet.FirstOrDefault(p => p.name == "capability.changeTrackingSupported")?.val;

            return (changeTrackingEnabled, changeTrackingSupported);

        }

        public async Task ConfigureForCBT(string vmMoRef)
        {
            if (session == null)
                throw new Exception("Not Logged");

            var taskRef = await Vim25Client.ReconfigVM_TaskAsync(
                    new ManagedObjectReference { type = "VirtualMachine", Value = vmMoRef },
                    new VirtualMachineConfigSpec { changeTrackingEnabled = true });

            var configTaskInfo = await WaitForTaskEnd(taskRef);

            if (configTaskInfo.state != TaskInfoState.success)
                throw new Exception(configTaskInfo.error.localizedMessage);

            var snapTask = await CreateSnapshot(vmMoRef, "Enable CBT", "", false, false);
            await RemoveSnapshot(snapTask, false, true);
        }

        public async Task<IEnumerable<ManagedEntity>> GetFolders()
        {
            if (session == null)
                throw new Exception("Not Logged");

            ////////////////////////////////////////////////////////

            var dc2vmFolder = new TraversalSpec
            {
                type = "Datacenter",
                path = "vmFolder",
                selectSet = new SelectionSpec[] { new SelectionSpec { name = "folderTSpec" } }
            };

            var folderTS = new TraversalSpec
            {
                name = "folderTSpec",
                type = "Folder",
                path = "childEntity",
                selectSet = new SelectionSpec[] { new SelectionSpec { name = "folderTSpec" }, dc2vmFolder }
            };


            var ospec = new ObjectSpec
            {
                obj = serviceContent.rootFolder,
                skip = false,
                selectSet = new SelectionSpec[] { folderTS }
            };

            /////////////////////////////////////////////

            var dcSp = new PropertySpec
            {
                type = "Datacenter",
                all = false,
                pathSet = new string[] { "parent", "name" }
            };

            var folderSp = new PropertySpec
            {
                type = "Folder",
                all = false,
                pathSet = new string[] { "parent", "name" }
            };

            ////////////////////////////////////////////

            var fs = new PropertyFilterSpec { objectSet = new ObjectSpec[] { ospec }, propSet = new PropertySpec[] { dcSp, folderSp } };

            var vmProps = await Vim25Client.RetrievePropertiesAsync(serviceContent.propertyCollector, new PropertyFilterSpec[] { fs });

            return vmProps.returnval
                .Select(obj =>
                new ManagedEntity
                {
                    MoRef = obj.obj.Value,
                    Type = obj.obj.type,
                    Name = (string)obj.propSet.FirstOrDefault(o => o.name == "name")?.val,
                    Parent = ((ManagedObjectReference)obj.propSet.FirstOrDefault(o => o.name == "parent")?.val)?.Value
                });
        }

        public async Task<IEnumerable<ManagedEntity>> GetPools()
        {
            if (session == null)
                throw new Exception("Not Logged");

            ////////////////////////////////////////////////////////

            var dc2hostFolder = new TraversalSpec
            {
                type = "Datacenter",
                path = "hostFolder",
                selectSet = new SelectionSpec[] { new SelectionSpec { name = "ressourcesTSpec" } }
            };

            var cr2resourcePool = new TraversalSpec
            {
                type = "ComputeResource",
                path = "resourcePool",
                selectSet = new SelectionSpec[] { new SelectionSpec { name = "ressourcesTSpec" } }
            };

            var rp2rp = new TraversalSpec
            {
                type = "ResourcePool",
                path = "resourcePool",
                selectSet = new SelectionSpec[] { new SelectionSpec { name = "ressourcesTSpec" } }
            };

            var folderTS = new TraversalSpec
            {
                name = "ressourcesTSpec",
                type = "Folder",
                path = "childEntity",
                selectSet = new SelectionSpec[] { new SelectionSpec { name = "ressourcesTSpec" }, dc2hostFolder, cr2resourcePool, rp2rp }
            };


            var ospec = new ObjectSpec
            {
                obj = serviceContent.rootFolder,
                skip = false,
                selectSet = new SelectionSpec[] { folderTS }
            };

            /////////////////////////////////////////////

            var dcSp = new PropertySpec
            {
                type = "Datacenter",
                all = false,
                pathSet = new string[] { "parent", "name" }
            };

            var folderSp = new PropertySpec
            {
                type = "Folder",
                all = false,
                pathSet = new string[] { "parent", "name" }
            };

            var computeSp = new PropertySpec
            {
                type = "ComputeResource",
                all = false,
                pathSet = new string[] { "parent", "name" }
            };

            var rpSp = new PropertySpec
            {
                type = "ResourcePool",
                all = false,
                pathSet = new string[] { "parent", "name" }
            };

            ////////////////////////////////////////////

            var fs = new PropertyFilterSpec { objectSet = new ObjectSpec[] { ospec }, propSet = new PropertySpec[] { dcSp, folderSp, computeSp, rpSp } };

            var vmProps = await Vim25Client.RetrievePropertiesAsync(serviceContent.propertyCollector, new PropertyFilterSpec[] { fs });

            return vmProps.returnval
                .Select(obj => 
                new ManagedEntity
                {
                    MoRef = obj.obj.Value,
                    Type = obj.obj.type,
                    Name = (string)obj.propSet.FirstOrDefault(o => o.name == "name")?.val,
                    Parent = ((ManagedObjectReference)obj.propSet.FirstOrDefault(o => o.name == "parent")?.val)?.Value
                });
        }


        public async Task<IEnumerable<ManagedEntity>> GetVMs()
        {
            if (session == null)
                throw new Exception("Not Logged");

            ////////////////////////////////////////////////////////

            var dc2vmFolder = new TraversalSpec
            {
                type = "Datacenter",
                path = "vmFolder",
                selectSet = new SelectionSpec[] { new SelectionSpec { name = "folderTSpec" } }
            };

            var folderTS = new TraversalSpec
            {
                name = "folderTSpec",
                type = "Folder",
                path = "childEntity",
                selectSet = new SelectionSpec[] { new SelectionSpec { name = "folderTSpec" }, dc2vmFolder }
            };

            var ospec = new ObjectSpec
            {
                obj = serviceContent.rootFolder,
                skip = false,
                selectSet = new SelectionSpec[] { folderTS }
            };

            /////////////////////////////////////////////

            var vmSp = new PropertySpec
            {
                type = "VirtualMachine",
                all = false,
                pathSet = new string[] { "name" }
            };

            ////////////////////////////////////////////

            var fs = new PropertyFilterSpec { objectSet = new ObjectSpec[] { ospec }, propSet = new PropertySpec[] { vmSp } };

            var vmProps = await Vim25Client.RetrievePropertiesAsync(serviceContent.propertyCollector, new PropertyFilterSpec[] { fs });

            return vmProps.returnval
                .Select(obj => new ManagedEntity { MoRef = obj.obj.Value, Type = obj.obj.type, Name = (string)obj.propSet[0].val });
        }

        private async Task<TaskInfo> WaitForTaskEnd(ManagedObjectReference task)
        {
            if (session == null)
                throw new Exception("Not Logged");

            if (task.type != "Task")
                throw new ArgumentException($"{nameof(task)} is not of type \"Task\"");

            var propertySpecTask = new PropertySpec
            {
                pathSet = new string[] { "info" },
                type = "Task"
            };
            var propertyFilterSpecTask = new PropertyFilterSpec
            {
                propSet = new PropertySpec[] { propertySpecTask },
                objectSet = new ObjectSpec[] { new ObjectSpec { obj = task } }
            };

            var taskProps = await Vim25Client.RetrievePropertiesAsync(serviceContent.propertyCollector, new PropertyFilterSpec[] { propertyFilterSpecTask });
            var configTaskInfo = taskProps.returnval[0].propSet[0].val as TaskInfo;
            while (configTaskInfo?.state == TaskInfoState.running)
            {
                await Task.Delay(2000);
                taskProps = await Vim25Client.RetrievePropertiesAsync(serviceContent.propertyCollector, new PropertyFilterSpec[] { propertyFilterSpecTask });
                configTaskInfo = taskProps.returnval[0].propSet[0].val as TaskInfo;
            };

            return configTaskInfo;

        }


        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if(session != null)
                    {
                        try
                        {
                            Logout().Wait();
                            (Vim25Client as IDisposable).Dispose();
                        }
                        catch { }
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Proxy() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}
