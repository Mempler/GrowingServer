using System.Threading.Tasks;
using GTServ.Pool;
using GTServ.RTSoft;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;

namespace GTServ
{
    internal static class Program
    {
        public static void ConfigureServices(IServiceCollection collection)
        {
            collection
                .AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                    loggingBuilder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                    loggingBuilder.AddNLog(LogManager.Configuration);
                })
                .AddScoped<StartupService>()
                .AddScoped<ServerPool>()
                .AddScoped<ItemDatabase>()
                .AddScoped<EventManager>()
                .AddScoped<WorldManager>()
                ;
        }
            
        private static async Task Main()
        {
            var serviceProvider = new ServiceCollection();
            ConfigureServices(serviceProvider);

            var provider = serviceProvider.BuildServiceProvider();
            
            var startupService = provider.GetService<StartupService>();

            await startupService.Run();
        }
    }
}