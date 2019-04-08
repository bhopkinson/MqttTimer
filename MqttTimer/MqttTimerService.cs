using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IMQTTClientRx.Service;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MqttTimer
{
    public class MqttTimerService : IHostedService
    {
        private readonly IMQTTService _mqttService;
        private readonly ILogger _logger;

        public MqttTimerService(
            IMQTTService mqttService,
            ILogger<MqttTimerService> logger)
        {
            _mqttService = mqttService;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Mqtt Timer Service is starting.");

            

            
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Mqtt Timer Service is stopping.");

            return Task.CompletedTask;
        }

        private async Task ConnectToMqtt()
        {
            _logger.LogInformation("Connecting to MQTT Broker");


        }
    }
}
