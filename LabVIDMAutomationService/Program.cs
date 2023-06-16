using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Net;

namespace LabVIDMAutomationService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                                | SecurityProtocolType.Tls11
                                                | SecurityProtocolType.Tls12
                                                | SecurityProtocolType.Ssl3;

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
			{ 
				new vIDMAutomationService() 
			};
            ServiceBase.Run(ServicesToRun);
        }
    }
}
