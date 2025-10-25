using Microsoft.Extensions.Options;
using RabbitMq.Shared.Infrastracture.Abstraction;
using RabbitMq.Shared.Settings;
using RabbitMQ.Client;

namespace RabbitMq.Shared.Infrastracture
{
    public class RabbitMqConnectionProvider(IOptions<RabbitMqSettings> rabbitMqOptions) : IRabbitMqConnectionProvider
    {
        private readonly RabbitMqSettings _rabbitMqSettings = rabbitMqOptions.Value;
        private IConnection? _connection;

        private readonly SemaphoreSlim _connectionLock = new(1, 1);

        public async Task<IConnection> GetConnectionAsync(CancellationToken stoppingToken)
        {
            if (_connection != null && _connection.IsOpen)
            {
                return _connection;
            }

            await _connectionLock.WaitAsync(stoppingToken);

            try
            {
                if (_connection != null && _connection.IsOpen)
                {
                    return _connection;
                }

                var factory = new ConnectionFactory { HostName = _rabbitMqSettings.HostName };
                _connection = await factory.CreateConnectionAsync(stoppingToken);

                return _connection;
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _connectionLock?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
