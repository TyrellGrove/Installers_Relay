using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace Installer_Relay_Windows_Service
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            #if DEBUG
                Service1 myWindowsService = new Service1();
                myWindowsService.OnDebug();
                System.Threading.Thread.Sleep(System.Threading.Timeout.Infinite);
            
            #else

                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new Service1()
                };
                ServiceBase.Run(ServicesToRun);
            #endif
        }
    }
}
