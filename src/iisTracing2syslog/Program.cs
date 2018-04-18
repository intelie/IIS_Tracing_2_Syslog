using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace iisTracing2syslog
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main()
		{
            // Run as service
            //ServiceBase[] ServicesToRun;
            //ServicesToRun = new ServiceBase[]
            //{
            //	new Service1()
            //};
            //ServiceBase.Run(ServicesToRun);

            // Run as console program (test)
            var dummyService = new Service1();
            dummyService.StartMonitoring(new DirectoryInfo("C:\\temp\\logs"));
            Console.WriteLine("Press \'q\' to quit the sample.");
            while (Console.Read() != 'q') ;        
        }
	}
}
