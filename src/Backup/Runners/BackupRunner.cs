using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BackupDatabase.Models;

namespace Backup.Runners
{
    public abstract class BackupRunner : IDisposable
    {
        protected CancellationTokenSource ctokenCancelBackup = new CancellationTokenSource();
        protected CancellationToken ctoken;
        protected DBBackup backup = null;

        protected void CheckCancelStatus()
        {
            ctoken.ThrowIfCancellationRequested();
            ctokenCancelBackup.Token.ThrowIfCancellationRequested();
        }

        public abstract void Dispose();
    }
}
