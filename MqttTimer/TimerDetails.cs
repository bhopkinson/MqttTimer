using System;

namespace MqttTimer
{
    public class TimerDetails
    {
        public string Name { get; set; }

        public CommandType Command { get; set; }

        public long UnixTriggerTimeSeconds { get; set; }

        public string ResponsePayload { get; set; }

        public DateTimeOffset TriggerDateTimeOffset => DateTimeOffset.FromUnixTimeSeconds(UnixTriggerTimeSeconds);

        public long DelaySecondsFromNow() => UnixTriggerTimeSeconds - DateTimeOffset.Now.ToUnixTimeSeconds();
    }
}
