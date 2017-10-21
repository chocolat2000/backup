using BackupService.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace BackupService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {

            var ServicesToRun = new ServiceBase[]
            {
                new BCKService()
            };
            ServiceBase.Run(ServicesToRun);

            //(new BCKService()).ForceStart();
            //Console.ReadLine();
        }
    }
}
