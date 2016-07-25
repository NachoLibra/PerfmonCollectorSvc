using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace PerfmonCollectorSvc
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        //static void Main()
        //{
        //    ServiceBase[] ServicesToRun;
        //    ServicesToRun = new ServiceBase[] 
        //    { 
        //        new Service1() 
        //    };
        //    ServiceBase.Run(ServicesToRun);
        //}
        static void Main()
        {
            #if (!DEBUG)
                System.ServiceProcess.ServiceBase[] ServicesToRun;
                ServicesToRun = new System.ServiceProcess.ServiceBase[] { new PerfmonCollectorSvc() };
                System.ServiceProcess.ServiceBase.Run(ServicesToRun);
            #else
                // Debug code: this allows the process to run as a non-service.
                // It will kick off the service start point, but never kill it.
                // Shut down the debugger to exit
                PerfmonCollectorSvc service = new PerfmonCollectorSvc();
                service.Start();
                System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
            #endif
        }
    }
}
