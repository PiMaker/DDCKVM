using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;

namespace DDCKVMService
{
    public class ConfigManager
    {
        private static readonly string ConfigPath = Path.Combine(Assembly.GetExecutingAssembly().Location.Substring(0, Assembly.GetExecutingAssembly().Location.LastIndexOf(Path.DirectorySeparatorChar)), "ddc_config.json");

        private static ConfigManager singleton;
        public static ConfigManager Singleton => singleton ?? (singleton = CreateConfigManager());

        private static ConfigManager CreateConfigManager()
        {
            if (File.Exists(ConfigPath))
            {
                try
                {
                    return JsonConvert.DeserializeObject<ConfigManager>(File.ReadAllText(ConfigPath));
                }
                catch (Exception ex)
                {
                    // Something happened, try and write log
                    // (If this fails too then we're borked anyway)
                    File.WriteAllText($"{ConfigPath}_load_error_{DateTime.Now.ToString("yyyyMMdd_hhmmss")}.log", JsonConvert.SerializeObject(ex));
                }
            }

            // Return empty/invalid on error or no config found
            return new ConfigManager();
        }

        [JsonIgnore]
        public bool IsValid =>
            this.Data != null &&
            this.Data.Length > 0 &&
            !string.IsNullOrEmpty(this.UsbIdentifier);

        public WebController.JSONDisplayForSaving[] Data { get; set; }

        public string UsbIdentifier { get; set; }

        public void Save()
        {
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
    }
}