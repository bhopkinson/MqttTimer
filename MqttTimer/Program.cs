using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MqttTimer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var hostBuilder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<MqttTimerService>();
                })
                .ConfigureLogging((hostContext, logging) =>
                {
                    logging.AddConsole();
                });

            await hostBuilder.RunConsoleAsync();
        }
    }
}
