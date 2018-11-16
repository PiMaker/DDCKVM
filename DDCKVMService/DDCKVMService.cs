using System;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Constants;
using Unosquare.Labs.EmbedIO.Modules;

namespace DDCKVMService
{
    public partial class DDCKVMService : ServiceBase
    {
        public static DateTime LastUSBConfigureTriggered = new DateTime(0);

        public static object usbEventLock = new object();

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private USBHandler usbHandler;

        private WebServer server;

        public DDCKVMService()
        {
            this.InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            // Set up local management web server
            this.server = new WebServer("http://localhost:4280/", RoutingStrategy.Regex);

            // Static files
            this.server.RegisterModule(new ResourceFilesModule(Assembly.GetExecutingAssembly(), "DDCKVMService.Resources"));

            // API
            this.server.RegisterModule(new WebApiModule());
            this.server.Module<WebApiModule>().RegisterController<WebController>();

            // Start server
            this.server.RunAsync(this.cancellationTokenSource.Token);

            // Register USB events
            this.usbHandler = new USBHandler();
            this.usbHandler.DeviceInserted += this.UsbHandlerGenerator("primary");
            this.usbHandler.DeviceRemoved += this.UsbHandlerGenerator("secondary");
        }

        // Generates an event handler that sets a display to a different input source when a configured USB device is removed or inserted.
        private EventHandler<string> UsbHandlerGenerator(string targetSource)
        {
            return (sender, e) =>
            {
                lock (usbEventLock)
                {
                    // Check if we're within timeout for a configure process
                    // If yes, abort
                    if ((DateTime.Now - LastUSBConfigureTriggered).TotalSeconds < 20)
                    {
                        return;
                    }

                    if (ConfigManager.Singleton.IsValid && ConfigManager.Singleton.UsbIdentifier == e)
                    {
                        // Call user process to switch source
                        WebController.CallUserSession("switch_" + targetSource).Wait();
                    }
                }
            };
        }

        protected override void OnStop()
        {
            this.cancellationTokenSource.Cancel();
        }
    }
}