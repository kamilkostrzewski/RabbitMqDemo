using RabbitMq.Shared.Settings.Abstraction;

namespace RabbitMq.Shared.Settings
{
    /// <summary>
    /// Stores the configuration settings required to connect
    /// to the RabbitMQ server and a specific queue.
    /// </summary>
    public class RabbitMqSettings: ISettings
    {
        /// <summary>
        /// The name of the configuration section in appsettings.json.
        /// </summary>
        public static string SectionName => "RabbitMq";

        /// <summary>
        /// The server hostname of the RabbitMQ instance 
        /// (e.g., "localhost" or "mq.mycompany.com").
        /// </summary>
        public required string HostName { get; init; }

        public required string ExchangeName { get; init; }
    }
}
