using System;
using System.Configuration.Install;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;

namespace iisTracing2syslog
{
    static class Program
	{
        private static readonly int CONSOLE_WAIT_TIME = 5000;

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
		{
            if (System.Environment.UserInteractive)
            {
                try
                {
                    AllocConsole();

                    if (args.Length == 1 && args[0] == "--install")
                    {
                        InstallService();
                        Thread.Sleep(CONSOLE_WAIT_TIME);
                    }
                    else if (args.Length == 1 && args[0] == "--uninstall")
                    {
                        UninstallService();
                        Thread.Sleep(CONSOLE_WAIT_TIME);
                    }
                    else if (args.Length == 1 && args[0] == "--console")
                    {
                        RunInConsole();
                    }
                    else 
                    {
                        Console.Error.WriteLine("Use one of the arguments: --install | --uninstall | --console");
                        Thread.Sleep(CONSOLE_WAIT_TIME);
                        Environment.Exit(1);
                        return;
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("Error! " + e);
                    Console.Error.WriteLine(e.StackTrace);
                    Thread.Sleep(CONSOLE_WAIT_TIME);
                    Environment.Exit(2);
                }
            } else
            {
                // No interactive environment - Regular service startup
                ServiceBase.Run(new IISTracing2Syslog());
            }
        }

        private static void RunInConsole()
        {
            Console.WriteLine("Starting in console mode");

            var dummyService = new IISTracing2Syslog();
            dummyService.StartStandalone();

            Console.WriteLine("Press \'q\' to quit the sample.");
            while (Console.Read() != 'q') ;

            dummyService.StopStandalone();
        }

        private static void InstallService()
        {
            ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
        }

        private static void UninstallService()
        {
            ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
        }

    }
}
