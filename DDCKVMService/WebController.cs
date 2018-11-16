using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unosquare.Labs.EmbedIO;
using Unosquare.Labs.EmbedIO.Constants;
using Unosquare.Labs.EmbedIO.Modules;

namespace DDCKVMService
{
    public class WebController : WebApiController
    {
        public WebController(IHttpContext context)
            : base(context)
        {
        }

        private static readonly Regex modelRegex = new Regex(@"model\((.*?)\)");
        private static readonly Regex sourcesRegex = new Regex(@"(?<=\s)60\((.*?)\)");

        // Basic handlers
        [WebApiHandler(HttpVerbs.Get, "/enum")]
        public async Task<bool> Enum()
        {
            return this.JsonResponse(await CallUserSession("enum") ?? "{\"success\":false}");
        }

        [WebApiHandler(HttpVerbs.Get, "/test")]
        public async Task<bool> Test()
        {
            await CallUserSession("switch_secondary");
            await Task.Delay(10 * 1000);
            await CallUserSession("switch_primary");

            return this.JsonResponse("{\"success\":true}");
        }

        [WebApiHandler(HttpVerbs.Post, "/save")]
        public bool Save()
        {
            var data = JsonConvert.DeserializeAnonymousType(this.RequestBody(), new { displays = new WebController.JSONDisplayForSaving[0] });
            ConfigManager.Singleton.Data = data.displays;
            ConfigManager.Singleton.Save();

            return this.JsonResponse("{\"success\":true}");
        }

        [WebApiHandler(HttpVerbs.Get, "/usb")]
        public async Task<bool> Usb()
        {
            var tempHandler = new USBHandler();

            try
            {
                var usbId = "";
                var usbIdLock = new object();

                // Register USB event that simply sets usbId to the USB device id of the changed device
                EventHandler<string> idExtractor = (sender, id) =>
                {
                    lock (usbIdLock)
                    {
                        usbId = id;
                    }
                };
                tempHandler.DeviceInserted += idExtractor;
                tempHandler.DeviceRemoved += idExtractor;

                // Wait for user to plug in or remove USB device
                var startTime = DateTime.Now;
                DDCKVMService.LastUSBConfigureTriggered = startTime;
                while (true)
                {
                    if ((DateTime.Now - startTime).TotalSeconds >= 20)
                    {
                        return this.JsonResponse("{\"usb\":\"Timeout, configured USB device unchanged!\"}");
                    }

                    lock (usbIdLock)
                    {
                        if (!string.IsNullOrEmpty(usbId))
                        {
                            break;
                        }
                    }

                    await Task.Delay(25);
                }

                // Reset timeout counter and save config
                DDCKVMService.LastUSBConfigureTriggered = DateTime.Now - TimeSpan.FromSeconds(17);
                ConfigManager.Singleton.UsbIdentifier = usbId;
                ConfigManager.Singleton.Save();

                return this.JsonResponse("{\"usb\":\"" + usbId.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"}");
            }
            finally
            {
                // Deregister temporary USB event handler
                tempHandler.Deregister();
            }
        }

        // Below is the HTTP-based callback method for user session processes
        public static string endpointReceiveData = null;

        private static readonly object endpointReceiveDataLock = new object();

        [WebApiHandler(HttpVerbs.Post, "/api/internal")]
        public bool UserSessionEndpoint()
        {
            endpointReceiveData = this.RequestBody();

            return this.JsonResponse("{}");
        }

        // User session helpers
        private static readonly object userSessionLock = new object();

        private static bool userSessionRunning = false;

        public static async Task<string> CallUserSession(string command)
        {
            var userTokenHandle = IntPtr.Zero;

            // Lock this method off so only one user process is called at a time
            while (true)
            {
                lock (userSessionLock)
                {
                    if (!userSessionRunning)
                    {
                        // Reset variable in lock to allow only one call to pass this barrier at a time
                        userSessionRunning = true;
                        break;
                    }
                }

                await Task.Delay(5);
            }

            // Prepare user context switch
            try
            {
                WindowsApi.WTSQueryUserToken(WindowsApi.WTSGetActiveConsoleSessionId(), ref userTokenHandle);

                var procInfo = new WindowsApi.PROCESS_INFORMATION();
                var startInfo = new WindowsApi.STARTUPINFOW();
                startInfo.cb = (uint)Marshal.SizeOf(startInfo);

                // Execute self in user session
                WindowsApi.CreateProcessAsUser(userTokenHandle, IntPtr.Zero, "\"" + Assembly.GetExecutingAssembly().Location + "\" cmd " + command, IntPtr.Zero, IntPtr.Zero, false, 0, IntPtr.Zero, null, ref startInfo, out procInfo);

                var startTime = DateTime.Now;
                while (true)
                {
                    // Lock against multi-threaded HTTP handling
                    lock (endpointReceiveDataLock)
                    {
                        if (endpointReceiveData != null)
                        {
                            var s = endpointReceiveData;
                            return s;
                        }
                    }

                    // Wait for endpoint
                    await Task.Delay(10);

                    if ((DateTime.Now - startTime).TotalSeconds > 10)
                    {
                        // Timeout
                        return null;
                    }
                }
            }
            finally
            {
                // Cleanup native handle
                if (userTokenHandle != IntPtr.Zero)
                {
                    WindowsApi.CloseHandle(userTokenHandle);
                }

                // Release lock to allow other calls to this function
                lock (userSessionLock)
                {
                    userSessionRunning = false;
                }
            }
        }

        // This function will run as logged on shell/console user
        public static void TriggerCmdInSession(string cmd)
        {
            if (cmd == "enum")
            {
                var retval = new { displays = new List<JSONDisplay>() };

                // Enumerate physical displays and return input source information
                MonitorController.GetDevices(handles =>
                {
                    foreach (var handle in handles)
                    {
                        var caps = handle.GetVCPCapabilities();
                        if (caps != null)
                        {
                            try
                            {
                                var model = modelRegex.Match(caps).Groups[1].Value;
                                var sources = sourcesRegex.Match(caps).Groups[1].Value.Split(' ').Select(x => Convert.ToUInt32(x, 16));
                                handle.GetVCPRegister(0x60, out uint currentSource);

                                retval.displays.Add(new JSONDisplay
                                {
                                    // NR is NOT a unique identification!
                                    nr = handle.hPhysicalMonitor.ToInt64(),
                                    name = model,
                                    currentSource = currentSource,
                                    sources = sources.ToArray()
                                });
                            }
                            catch
                            {
                                // ignored
                                // We simply don't list displays with incompatible VCP caps
                                // (or if they don't support DDC/CI at all)
                            }
                        }
                    }
                });

                var json = JsonConvert.SerializeObject(retval);
                SendToSystemService(json).Wait();
            }
            else if (cmd == "switch_primary" || cmd == "switch_secondary")
            {
                // Device inserted, if we have a valid config, switch monitors to "Primary" source
                // or
                // Device removed, if we have a valid config, switch monitors to "Secondary" source
                MonitorController.GetDevices(handles =>
                {
                    foreach (var handle in handles)
                    {
                        var deviceConfig = ConfigManager.Singleton.Data.FirstOrDefault(x => x.nr == handle.hPhysicalMonitor.ToInt64());
                        if (deviceConfig != default(WebController.JSONDisplayForSaving))
                        {
                            // Config found, set VCP
                            handle.SetVCPRegister(0x60, cmd == "switch_primary" ? deviceConfig.primarySource : deviceConfig.secondarySource);
                        }
                    }
                });

                SendToSystemService("{\"status\":\"done\"}").Wait();
            }
            else
            {
                Console.WriteLine("Unknown command!");
            }
        }

        // Helper to send response back to system process. See /api/internal for more.
        private static async Task SendToSystemService(string data)
        {
            var client = new HttpClient();
            await client.PostAsync("http://localhost:4280/api/internal", new StringContent(data));
            client.Dispose();
        }

        public override void SetDefaultHeaders() => this.NoCache();

        public class JSONDisplay
        {
            // NR is NOT a unique identification!
            public long nr;

            public string name;
            public uint currentSource;
            public uint[] sources;
        }

        public class JSONDisplayForSaving
        {
            public long nr;
            public uint primarySource;
            public uint secondarySource;
        }
    }
}