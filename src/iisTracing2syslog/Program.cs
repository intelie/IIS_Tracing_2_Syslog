using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;

namespace iisTracing2syslog
{
	static class Program
	{
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
		{
            if (System.Environment.UserInteractive)
            {
                try
                {
                    AllocConsole();
                    Console.WriteLine("Starting in console mode");

                    var dummyService = new IISTracing2Syslog();
                    dummyService.StartStandalone();

                    Console.WriteLine("Press \'q\' to quit the sample.");
                    while (Console.Read() != 'q') ;

                    dummyService.StopStandalone();
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Error! " + e);
                    Console.Error.WriteLine(e.StackTrace);
                    Console.Read();
                }
            } else
            {                
                ServiceBase.Run(new IISTracing2Syslog());
            }
        }
	}
}
