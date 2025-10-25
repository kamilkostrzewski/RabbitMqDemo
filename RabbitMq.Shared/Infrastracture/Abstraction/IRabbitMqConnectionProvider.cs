using RabbitMQ.Client;

namespace RabbitMq.Shared.Infrastracture.Abstraction
{
    public interface IRabbitMqConnectionProvider : IDisposable
    {
        Task<IConnection> GetConnectionAsync(CancellationToken stoppingToken);
    }
}
