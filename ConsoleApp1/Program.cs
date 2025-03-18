using System;
using System.IO;
using System.Text.Json;

namespace DecoderApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var configPath = "Appsettings.json";
            if (!File.Exists(configPath))
            {
                Console.WriteLine("Configuration file not found.");
                return;
            }

            var configJson = await File.ReadAllTextAsync(configPath);
            var appSettings = JsonSerializer.Deserialize<AppSettings>(configJson);

            if (appSettings == null)
            {
                Console.WriteLine("Invalid configuration.");
                return;
            }

            // var app = new DecoderApp(appSettings);
            // await app.RunAsync();
            // var app = new DecoderAppAsync();
            // await DecoderAppAsync.Main();
            await Task.Run(() => new DecoderAppAsync());
        }
    }
}
