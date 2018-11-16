using Newtonsoft.Json;
using System;
using System.IO;
using System.ServiceProcess;

namespace DDCKVMService
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main()
        {
            try
            {
                var args = Environment.GetCommandLineArgs();
                if (args.Length == 3 && args[1] == "cmd")
                {
                    WebController.TriggerCmdInSession(args[2]);
                    return;
                }
            }
            catch (Exception ex)
            {
                File.WriteAllText(@"C:\Users\horse\Desktop\DDCKVMService\DDCKVMService\bin\Debug\ddcerr.log", JsonConvert.SerializeObject(ex));

                Console.WriteLine("An error occured during command");
                return;
            }

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new DDCKVMService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}