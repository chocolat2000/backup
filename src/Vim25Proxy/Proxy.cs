using System;
using System.IO;
using System.IO.Compression;
using System.Xml.Serialization;
using System.Net.Http;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using Vim25Api;

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

            var taskInfo = await WaitForTaskEnd(taskRef.Value);
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

            var taskInfo = await WaitForTaskEnd(taskRef.Value);
            if (taskInfo.state != TaskInfoState.success)
                throw new Exception(taskInfo.error.localizedMessage);

        }

        public async Task<object> GetVMConfigFromSnapshot(string snapshotMoRef)
        {
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

        public byte[] SerializeVMConfig(object vmConfig)
        {
            byte[] result = null;
            using (var memStream = new MemoryStream())
            {
                using (var zippedStream = new DeflateStream(memStream, CompressionMode.Compress))
                {
                    var xs = new XmlSerializer(vmConfig.GetType());
                    xs.Serialize(zippedStream, vmConfig);
                }
                result = memStream.ToArray();
            }

            return result;
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

            var configTaskInfo = await WaitForTaskEnd(taskRef.Value);

            if (configTaskInfo.state != TaskInfoState.success)
                throw new Exception(configTaskInfo.error.localizedMessage);

            var snapTask = await CreateSnapshot(vmMoRef, "Enable CBT", "", false, false);
            await RemoveSnapshot(snapTask, false, true);
        }

        public async Task<IDictionary<string, string>> GetVMs()
        {
            if (session == null)
                throw new Exception("Not Logged");

            ////////////////////////////////////////////////////////

            var dc2vmFolder = new TraversalSpec
            {
                type = "Datacenter",
                path = "vmFolder",
                selectSet = new SelectionSpec[] { new SelectionSpec() { name = "folderTSpec" } }
            };

            var folderTS = new TraversalSpec
            {
                name = "folderTSpec",
                type = "Folder",
                path = "childEntity",
                selectSet = new SelectionSpec[] { new SelectionSpec() { name = "folderTSpec" }, dc2vmFolder }
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
                .OrderBy(obj => (string)obj.propSet[0].val)
                .ToDictionary(obj => obj.obj.Value, obj => (string)obj.propSet[0].val);
        }

        private async Task<TaskInfo> WaitForTaskEnd(string taskMoRef)
        {
            if (session == null)
                throw new Exception("Not Logged");

            var propertySpecTask = new PropertySpec
            {
                pathSet = new string[] { "info" },
                type = "Task"
            };
            var propertyFilterSpecTask = new PropertyFilterSpec
            {
                propSet = new PropertySpec[] { propertySpecTask },
                objectSet = new ObjectSpec[] { new ObjectSpec { obj = new ManagedObjectReference { type = "Task", Value = taskMoRef } } }
            };

            TaskInfo configTaskInfo;
            do
            {
                await Task.Delay(2000);
                var taskProps = await Vim25Client.RetrievePropertiesAsync(serviceContent.propertyCollector, new PropertyFilterSpec[] { propertyFilterSpecTask });
                configTaskInfo = taskProps.returnval[0].propSet[0].val as TaskInfo;
            } while (configTaskInfo?.state == TaskInfoState.running);

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
