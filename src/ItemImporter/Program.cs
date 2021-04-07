// Copyright (C) 2021 Donovan Sullivan
// GNU General Public License v3.0+ (see LICENSE or https://www.gnu.org/licenses/gpl-3.0.txt)

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ValhallaLootList.ItemImporter
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseConsoleLifetime()
                .ConfigureHostConfiguration(config => config.AddUserSecrets(typeof(Program).Assembly))
                .ConfigureLogging(log => log.AddFile("Logs/ItemImporter-{Date}.txt", LogLevel.Warning))
                .ConfigureServices((hostContext, services) => new Startup(hostContext.Configuration).ConfigureServices(services));
    }
}
