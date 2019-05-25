using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using Newtonsoft.Json.Linq;

namespace MqttTimer
{
    public class MqttTimerService : IHostedService
    {
        private readonly IManagedMqttClient _mqttClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;

        private readonly ConcurrentDictionary<string, Timer> _timers = new ConcurrentDictionary<string, Timer>();
        private readonly Mutex _timersMutex = new Mutex();

        public MqttTimerService(
            IManagedMqttClient mqttClient,
            IConfiguration configuration,
            ILogger<MqttTimerService> logger)
        {
            _mqttClient = mqttClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Mqtt Timer Service is starting.");

            RegisterEvent();
            await StartMqttClient();
            await SubscribeToTopic("mqtttimer/+/start");
            await SubscribeToTopic("mqtttimer/+/stop");

        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Mqtt Timer Service is stopping.");

            await _mqttClient.StopAsync();
        }

        private void RegisterEvent()
        {
            _mqttClient.ApplicationMessageReceived += ApplicationMessageReceived;
        }

        private void ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            _timersMutex.WaitOne();

            try
            {
                var timerDetails = GetTimerDetails(e.ApplicationMessage);
                if (timerDetails == null)
                {
                    return;
                }

                _logger.LogInformation($"{timerDetails.Command.ToString()} command received for {timerDetails.Name} timer");

                switch (timerDetails.Command)
                {
                    case CommandType.Start:
                        ClearMqttTopicRetainedMessage(timerDetails).GetAwaiter().GetResult();
                        StartTimer(timerDetails);
                        break;

                    case CommandType.Stop:
                        StopTimer(timerDetails);
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }
            finally
            {
                _timersMutex.ReleaseMutex();
            }
        }

        private void StartTimer(TimerDetails timerDetails)
        {
            // Stop existing timer
            StopTimer(timerDetails);

            _logger.LogInformation($"Creating timer for {timerDetails.Name}");

            var delayInSeconds = timerDetails.DelaySecondsFromNow();
            _logger.LogInformation($"Setting timer for {timerDetails.Name} to {delayInSeconds} seconds");
            _timers[timerDetails.Name] = new Timer(TimerCallback, timerDetails, delayInSeconds * 1000, Timeout.Infinite);
        }

        private void StopTimer(TimerDetails timerDetails)
        {
            _timers.TryRemove(timerDetails.Name, out var timer);

            if (timer != null)
            {
                _logger.LogInformation($"Disposing timer for {timerDetails.Name}");
                timer.Dispose();
            }
        }

        private async void TimerCallback(object stateInfo)
        {
            _timersMutex.WaitOne();

            try
            {
                var timerDetails = (TimerDetails)stateInfo;
                var topic = $"mqtttimer/{timerDetails.Name}";

                _logger.LogInformation($"Timer callback for {timerDetails.Name} called.");
                _logger.LogInformation($"Publishing payload '{timerDetails.ResponsePayload}' to topic {topic}");

                var managedMqttApplicationMessage = new ManagedMqttApplicationMessageBuilder()
                        .WithApplicationMessage(new MqttApplicationMessageBuilder()
                            .WithTopic(topic)
                            .WithPayload(timerDetails.ResponsePayload)
                            .WithAtLeastOnceQoS()
                            .WithRetainFlag()
                            .Build())
                        .Build();

                await _mqttClient.PublishAsync(managedMqttApplicationMessage);
            }
            finally
            {
                _timersMutex.ReleaseMutex();
            }
        }

        private async Task ClearMqttTopicRetainedMessage(TimerDetails timerDetails)
        {
            _timersMutex.WaitOne();

            try
            {
                var topic = $"mqtttimer/{timerDetails.Name}";

                var managedMqttApplicationMessage = new ManagedMqttApplicationMessageBuilder()
                    .WithApplicationMessage(new MqttApplicationMessageBuilder()
                        .WithTopic(topic)
                        .WithAtLeastOnceQoS()
                        .WithRetainFlag()
                        .Build())
                    .Build();

                await _mqttClient.PublishAsync(managedMqttApplicationMessage);
            }
            finally
            {
                _timersMutex.ReleaseMutex();
            }
        }

        private async Task StartMqttClient()
        {
            _logger.LogInformation("Starting Mqtt Client.");

            var brokerUrl = _configuration.GetValue<string>("HOST");

            _logger.LogInformation($"Connecting to broker '{brokerUrl}'");

            var mqttManagedClientOptions = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(new MqttClientOptionsBuilder()
                    .WithTcpServer(brokerUrl)
                    .Build())
                .Build();
            
            await _mqttClient.StartAsync(mqttManagedClientOptions);
        }

        private async Task SubscribeToTopic(string topic)
        {
            _logger.LogInformation($"Subscribing to topic '{topic}'");

            var startTopicFilter = new TopicFilterBuilder()
                .WithTopic(topic)
                .WithAtLeastOnceQoS()
                .Build();

            await _mqttClient.SubscribeAsync(startTopicFilter);

            _logger.LogInformation("Subscribing to topic 'mqtttimer/+/stop'");
        }

        private TimerDetails GetTimerDetails(MqttApplicationMessage message)
        {
            var topic = message.Topic;
            var payload = message.ConvertPayloadToString();

            _logger.LogInformation($"Recevied message with Topic '{topic}', Payload '{payload}'");

            if (string.IsNullOrWhiteSpace(topic))
            {
                _logger.LogWarning("Ignoring message with empty topic");
                return null;
            }

            var topicParts = topic.Split('/');

            if (topicParts.Length != 3)
            {
                _logger.LogWarning("Ignoring message with unrecognised topic format.");
                return null;
            }

            var payloadJObject = JObject.Parse(payload);
            Enum.TryParse<CommandType>(topicParts[2], ignoreCase: true, out var command);
            return new TimerDetails
            {
                Name = topicParts[1],
                Command = command,
                UnixTriggerTimeSeconds = (long)payloadJObject["triggerTimeSeconds"],
                ResponsePayload = payloadJObject["responsePayload"].ToString()
            };
        }
    }
}
