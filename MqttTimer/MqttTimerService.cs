using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MqttTimer
{
    public class MqttTimerService : IHostedService
    {
        private readonly ILogger _logger;

        public MqttTimerService(ILogger<MqttTimerService> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Mqtt Timer Service is starting.");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Mqtt Timer Service is stopping.");

            return Task.CompletedTask;
        }
    }
}
