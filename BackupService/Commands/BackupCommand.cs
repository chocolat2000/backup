using BackupService.Services;
using Alphaleonis.Win32.Filesystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BackupNetworkLibrary.Model;
using BackupNetworkLibrary;
using System.Collections.Concurrent;

namespace BackupService.Commands
{
    public class BackupCommand : IDisposable
    {
        private VSSService vss = new VSSService();
        private ConcurrentDictionary<Guid, string> files;

        public delegate void SendBackupItemCallback(BackupItem item);
        public delegate void SendWarningLogCallback(string message);
        public delegate void BackupCompletedCallback();

        public SendBackupItemCallback SendBackupItem { get; set; }
        public SendWarningLogCallback SendWarningLog { get; set; }
        public BackupCompletedCallback BackupCompleted { get; set; }

        public void Initialize(ConcurrentDictionary<Guid, string> files)
        {
            this.files = files;
        }

        public void RunBackup(IEnumerable<string> items)
        {
            foreach (var volume in items.Select(item => Path.GetPathRoot(item)).Distinct())
                vss.AddVolume(volume);

            vss.DoSnapshot();


            foreach (var item in items)
            {
                try
                {
                    BackupItem(vss.GetSnapshotPath(item));
                }
                catch (Exception e) { }

            }

            BackupCompleted();

            
        }

        public void BackupComplete()
        {
            if(vss != null)
                vss.BackupComplete();
        }

        private void BackupItem(string item)
        {
            if (item.StartsWith("GLOBALROOT", StringComparison.Ordinal))
                item = "\\\\?\\" + item;

            if (Directory.Exists(item))
            {
                BackupFolder(item);
            }
            else
            {
                var slashPos = item.LastIndexOf(Path.DirectorySeparatorChar);
                if (slashPos > 0)
                {

                    var path = item.Remove(slashPos + 1);
                    var search = item.Substring(path.Length);

                    foreach (var searchItem in Directory.EnumerateFileSystemEntries(path, search))
                    {
                        var thisItem = searchItem;
                        if (searchItem.StartsWith("GLOBALROOT", StringComparison.Ordinal))
                            thisItem = "\\\\?\\" + searchItem;

                        if (Directory.Exists(thisItem))
                        {
                            BackupFolder(thisItem);
                        }
                        else if (File.Exists(thisItem))
                        {
                            BackupFile(thisItem);
                        }
                    }
                }
            }
        }

        private void BackupFolder(string folder)
        {
            var dirInfo = new DirectoryInfo(folder);
            var dirAcl = dirInfo.GetAccessControl();

            var original = vss.GetOriginalPath(folder);

            SendBackupItem(new BackupItem { Name = original, LastWriteTime = dirInfo.LastWriteTimeUtc, Type = BackupItemType.Folder });

            foreach (var item in Directory.EnumerateFileSystemEntries(folder))
            {
                BackupItem(item);
            }
        }

        private void BackupFile(string file)
        {
            var fileInfo = new FileInfo(file);
            var fileAcl = fileInfo.GetAccessControl();

            var original = vss.GetOriginalPath(file);


            Guid streamGuid = Guid.NewGuid();
            files.TryAdd(streamGuid, file);
            SendBackupItem(new BackupItem { Name = original, LastWriteTime = fileInfo.LastWriteTimeUtc, Type = BackupItemType.File, Length = fileInfo.Length, StreamGuid = streamGuid });


        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (vss != null)
                    {
                        vss.Dispose();
                        vss = null;
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
