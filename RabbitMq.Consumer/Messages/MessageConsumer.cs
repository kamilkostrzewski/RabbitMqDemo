using Microsoft.Extensions.Options;
using RabbitMq.Shared.Settings;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace RabbitMq.Consumer.Messages
{
    internal class MessageConsumer(IOptions<RabbitMqSettings> rabbitMqOptions) : BackgroundService, IDisposable
    {
        private readonly RabbitMqSettings _rabbitMqSettings = rabbitMqOptions.Value;
        private IConnection _connection = null!;
        private IChannel _channel = null!;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Message Consumer is starting...");

            var factory = CreateConnectionFactory();
            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
            await QueueDeclareAsync(_channel, stoppingToken);

            Console.WriteLine("Waiting for messages...");

            try
            {
                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (sender, eventArgs) =>
                {
                    byte[] body = eventArgs.Body.ToArray();
                    string message = Encoding.UTF8.GetString(body);

                    Console.WriteLine($"Received: {message}");

                    try
                    {
                        await Task.Delay(1000);
                        await ((AsyncEventingBasicConsumer)sender).Channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Failed to process message: {ex.Message}");
                        await ((AsyncEventingBasicConsumer)sender).Channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
                    }
                };

                await BasicConsumeAsync(_channel, consumer, stoppingToken);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Shutdown requested. Stopping consumer.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Consumer failed: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Consumer shutdown complete.");
            }
        }

        public override void Dispose()
        {
            try
            {
                _channel?.Dispose();
                _connection?.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Error during dispose: {ex.Message}");
            }

            base.Dispose();
        }

        private async Task QueueDeclareAsync(IChannel channel, CancellationToken stoppingToken)
        {
            await channel.QueueDeclareAsync(
                queue: _rabbitMqSettings.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);
        }

        private async Task BasicConsumeAsync(IChannel channel, IAsyncBasicConsumer consumer,  CancellationToken stoppingToken)
        {
            await channel.BasicConsumeAsync(
                _rabbitMqSettings.QueueName,
                autoAck: false,
                consumer,
                cancellationToken: stoppingToken);
        }

        private ConnectionFactory CreateConnectionFactory()
        {
            return new ConnectionFactory { HostName = _rabbitMqSettings.HostName };
        }
    }
}
