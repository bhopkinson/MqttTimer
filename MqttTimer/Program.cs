using System.Threading.Tasks;
using IMQTTClientRx.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTClientRx.Service;

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
                    services.AddSingleton<IMQTTService>(new MQTTService());
                })
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddEnvironmentVariables(prefix: "MQTT_");
                })
                .ConfigureLogging((hostContext, logging) =>
                {
                    logging.AddConsole();
                });

            await hostBuilder.RunConsoleAsync();
        }
    }
}
