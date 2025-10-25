using Microsoft.Extensions.Options;
using RabbitMq.Shared.Infrastracture.Abstraction;
using RabbitMq.Shared.Messages;
using RabbitMq.Shared.Settings;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace RabbitMq.Producer.Messages
{
    internal class MessageProducer(
        IRabbitMqConnectionProvider connectionProvider,
        IOptions<RabbitMqSettings> rabbitMqOptions) : BackgroundService
    {
        private readonly RabbitMqSettings _rabbitMqSettings = rabbitMqOptions.Value;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Message Producer is starting...");

            using var connection = await connectionProvider.GetConnectionAsync(stoppingToken);
            using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
            await ExchangeDeclareAsync(channel, stoppingToken);

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    var message = new MessagePayload("Title", "Body", DateTime.UtcNow);
                    var serializedMessage = JsonSerializer.Serialize(message);
                    var body = Encoding.UTF8.GetBytes(serializedMessage);

                    await channel.BasicPublishAsync(
                        exchange: _rabbitMqSettings.ExchangeName,
                        routingKey: string.Empty,
                        mandatory: true,
                        basicProperties: new BasicProperties { Persistent = true },
                        body: body,
                        cancellationToken: stoppingToken);

                    Console.WriteLine($"Sent {message}");
                    await Task.Delay(2000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Shutdown requested. Stopping producer.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Producer failed: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Producer shutdown complete.");
            }
        }

        private async Task ExchangeDeclareAsync(IChannel channel, CancellationToken stoppingToken)
        {
            await channel.ExchangeDeclareAsync(
                   exchange: _rabbitMqSettings.ExchangeName,
                   type: ExchangeType.Fanout,
                   durable: true,
                   autoDelete: false,
                   arguments: null,
                   cancellationToken: stoppingToken);
        }
    }
}
