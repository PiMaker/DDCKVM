using DDCKVMService;
using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace DDCKVMTesting
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Starting test...");

            Console.WriteLine(Path.Combine(Assembly.GetExecutingAssembly().Location.Substring(0, Assembly.GetExecutingAssembly().Location.LastIndexOf(Path.DirectorySeparatorChar)), "ddc_config.json"));

            var modelRegex = new Regex(@"model\((.*?)\)");
            var sourcesRegex = new Regex(@"(?<=\s)60\((.*?)\)");

            MonitorController.GetDevices(handles =>
            {
                foreach (var h in handles)
                {
                    Console.WriteLine("Display found: " + h.szPhysicalMonitorDescription + " (" + h.hPhysicalMonitor.ToInt64() + ")");
                    var caps = h.GetVCPCapabilities();
                    Console.WriteLine(caps);
                    Console.WriteLine(" => Model: " + modelRegex.Match(caps).Groups[1].Value);
                    Console.WriteLine(" => Supported sources: " + string.Join("; ", sourcesRegex.Match(caps).Groups[1].Value.Split(' ')));
                }
            });

            Console.WriteLine("Starting USB test...");
            var usbHandler = new USBHandler();

            Console.WriteLine("Press any key to end test, USB actions will be displayed now.");
            Console.ReadKey(true);

            Console.WriteLine("Test done!");
        }
    }
}