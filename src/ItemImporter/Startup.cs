// Copyright (C) 2021 Donovan Sullivan
// GNU General Public License v3.0+ (see LICENSE or https://www.gnu.org/licenses/gpl-3.0.txt)

using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ValhallaLootList.ItemImporter.WarcraftDatabase;

namespace ValhallaLootList.ItemImporter
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHostedService<App>();

            services.Configure<Config>(config =>
            {
                config.SeedInstancesPath = Configuration.GetValue<string>(nameof(config.SeedInstancesPath));
                config.SeedItemsPath = Configuration.GetValue<string>(nameof(config.SeedItemsPath));

                foreach (var section in Configuration.GetSection(nameof(config.Tokens)).GetChildren())
                {
                    config.Tokens[uint.Parse(section.Key)] = section.GetChildren().Select(child => uint.Parse(child.Value)).ToArray();
                }
            });

            services.AddDbContext<WowDataContext>(options =>
                options.UseMySql(Configuration.GetConnectionString("WowConnection"), MySqlServerVersion.LatestSupportedServerVersion));
        }
    }
}
