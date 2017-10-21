using Alphaleonis.Win32.Vss;
using Alphaleonis.Win32.Filesystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BackupService.Services
{
    public class VSSService : IDisposable
    {
        private static string slash = Alphaleonis.Win32.Filesystem.Path.DirectorySeparatorChar.ToString();

        private IVssBackupComponents backup;
        private IDictionary<string, Guid> snapShotsGuids = new Dictionary<string, Guid>();
        private IDictionary<string, string> snapShotsRoots = new Dictionary<string, string>();
        private Guid snapShotSet;

        public VSSService()
        {
            backup = VssUtils.LoadImplementation().CreateVssBackupComponents();

            backup.InitializeForBackup(null);

            snapShotSet = backup.StartSnapshotSet();

            // TODO : per component backup
            //backup.GatherWriterMetadata();
        }

        public void AddVolume(string volumeName)
        {
            try
            {
                snapShotsGuids.Keys.First(vol => vol.Equals(volumeName));
            }
            catch
            {

                if (backup.IsVolumeSupported(volumeName))
                {
                    var snapGuid = backup.AddToSnapshotSet(volumeName);
                    snapShotsGuids.Add(volumeName, snapGuid);
                }
                else
                    throw new VssVolumeNotSupportedException(volumeName);
            }
        }

        public void DoSnapshot()
        {
            backup.SetBackupState(false, true, VssBackupType.Full, false);
            backup.PrepareForBackup();
            backup.DoSnapshotSet();
            foreach(var vol in snapShotsGuids.Keys)
            {
                snapShotsRoots.Add(GetSnapshotRoot(vol), vol);
            }
        }

        public void BackupComplete()
        {
            try
            {
                backup.BackupComplete();
            }
            // Not sure why, but this throws a VSS_BAD_STATE on XP and W2K3.
            // Per some forum posts about this, I'm just ignoring it.
            catch (VssBadStateException) { }
        }

        public string GetSnapshotRoot(string volumeName)
        {
            try
            {
                var snapProperties = backup.GetSnapshotProperties(snapShotsGuids[volumeName]);
                return snapProperties.SnapshotDeviceObject;
            }
            catch
            {
                return null;
            }
        }

        public string GetOriginalPath(string snapPath)
        {
            var root = snapShotsRoots.First(keypair => 
                snapPath.StartsWith(keypair.Key, StringComparison.Ordinal)
            );

            if(root.Key.EndsWith(slash, StringComparison.Ordinal))
                snapPath = snapPath.Remove(0, root.Key.Length);
            else
                snapPath = snapPath.Remove(0, root.Key.Length + 1);

            return root.Value + snapPath;
        }

        public string GetSnapshotPath(string localPath)
        {
            string root = Alphaleonis.Win32.Filesystem.Path.GetPathRoot(localPath);
            localPath = localPath.Remove(0, root.Length);
            
            string snapRoot = GetSnapshotRoot(root);

            if (!snapRoot.EndsWith(slash, StringComparison.Ordinal) && !localPath.StartsWith(slash, StringComparison.Ordinal))
            {
                localPath = snapRoot + slash + localPath;
            }
            else
            {
                localPath = snapRoot + localPath;
            }

            return localPath;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    BackupComplete();

                    if (snapShotSet != Guid.Empty)
                    {
                        backup.DeleteSnapshotSet(snapShotSet, true);
                        snapShotSet = Guid.Empty;
                    }

                    if (backup != null)
                    {
                        backup.Dispose();
                        backup = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~RunnerClass() {
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
