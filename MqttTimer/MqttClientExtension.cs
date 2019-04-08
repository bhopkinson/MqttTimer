using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Collections.Generic;
using System.Text;

namespace MqttTimer
{
    public class MqttClientExtension
    {
        public static IServiceCollection AddManagedMqttClient(this IServiceCollection services, IMqttClientOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            services.AddSingleton(options);
        }
    }
}
