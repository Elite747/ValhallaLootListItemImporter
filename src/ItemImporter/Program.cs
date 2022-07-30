// Copyright (C) 2021 Donovan Sullivan
// GNU General Public License v3.0+ (see LICENSE or https://www.gnu.org/licenses/gpl-3.0.txt)

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ValhallaLootList.ItemImporter;
using ValhallaLootList.ItemImporter.WarcraftDatabase;

var configuration = new ConfigurationBuilder()
    .AddCommandLine(args)
    .AddUserSecrets<SeedItem>()
    .AddJsonFile("appsettings.json")
    .Build();

await using var services = new ServiceCollection()
    .AddLogging(log => log.AddFile("Logs/ItemImporter-{Date}.txt", LogLevel.Warning).AddConsole())
    .Configure<Config>(config =>
    {
        config.SeedInstancesPath = configuration.GetValue<string>(nameof(config.SeedInstancesPath));
        config.SeedItemsPath = configuration.GetValue<string>(nameof(config.SeedItemsPath));

        foreach (var section in configuration.GetSection(nameof(config.Tokens)).GetChildren())
        {
            config.Tokens[uint.Parse(section.Key)] = section.GetChildren().Select(child => uint.Parse(child.Value)).ToArray();
        }
    })
    .AddDbContext<TypedDataContext>(options => options.UseMySql(configuration.GetConnectionString("WowConnection"), MySqlServerVersion.LatestSupportedServerVersion))
    .AddTransient<App>()
    .BuildServiceProvider();

await services.GetRequiredService<App>().StartAsync(default);
