using BackupService.Services;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace BackupService
{
    public partial class BCKService : ServiceBase
    {
        private ServiceHost backupServiceHost;
        private ServiceHost generalServiceHost;
        private ServiceHost streamServiceHost;

        public BCKService()
        {
            InitializeComponent();
        }

        public void ForceStart()
        {
            OnStart(new string[0]);
        }

        protected override void OnStart(string[] args)
        {
#if DEBUG
            Debugger.Launch();
#endif

            var files = new ConcurrentDictionary<Guid, string>();
            var backupService = new Services.BackupService(files);
            var generalService = new Services.GeneralService();
            var streamService = new Services.StreamService(files);

            try
            {
                backupServiceHost = new ServiceHost(backupService);
                generalServiceHost = new ServiceHost(generalService);
                streamServiceHost = new ServiceHost(streamService);

                // Security configuration
                //generalServiceHost.Authorization.ImpersonateCallerForAllOperations = true;


                var backupThread = new System.Threading.Thread(new System.Threading.ThreadStart(backupServiceHost.Open));
                var generalThread = new System.Threading.Thread(new System.Threading.ThreadStart(generalServiceHost.Open));
                var streamThread = new System.Threading.Thread(new System.Threading.ThreadStart(streamServiceHost.Open));
                backupThread.Start();
                generalThread.Start();
                streamThread.Start();
            }
            catch (Exception e)
            {
                backupServiceHost = null;
                generalServiceHost = null;
                streamServiceHost = null;
            }

        }

        protected override void OnStop()
        {
            if (backupServiceHost != null)
                backupServiceHost.Close();

            if (generalServiceHost != null)
                generalServiceHost.Close();

            if (streamServiceHost != null)
                streamServiceHost.Close();
        }
    }
}
