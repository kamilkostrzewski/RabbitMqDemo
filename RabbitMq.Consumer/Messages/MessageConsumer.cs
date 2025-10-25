using Microsoft.Extensions.Options;
using RabbitMq.Shared.Infrastracture.Abstraction;
using RabbitMq.Shared.Messages;
using RabbitMq.Shared.Settings;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace RabbitMq.Consumer.Messages
{
    internal class MessageConsumer(
        IRabbitMqConnectionProvider connectionProvider,
        IOptions<RabbitMqSettings> rabbitMqOptions) : BackgroundService, IDisposable
    {
        private readonly RabbitMqSettings _rabbitMqSettings = rabbitMqOptions.Value;

        private IConnection _connection = null!;
        private IChannel _channel = null!;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Message Consumer is starting...");

            _connection = await connectionProvider.GetConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            await ExchangeDeclareAsync(_channel, stoppingToken);
            var queueDeclareResult = await QueueDeclareAsync(_channel, stoppingToken);
            var queueName = queueDeclareResult.QueueName;
            await QueueBindAsync(_channel, queueName, stoppingToken);

            Console.WriteLine("Waiting for messages...");

            try
            {
                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += async (sender, eventArgs) =>
                {
                    var channel = ((AsyncEventingBasicConsumer)sender).Channel;

                    try
                    {
                        var body = eventArgs.Body.ToArray();
                        var messageJson = Encoding.UTF8.GetString(body);
                        var messagePayload = JsonSerializer.Deserialize<MessagePayload?>(messageJson)
                            ?? throw new JsonException("Deserialized payload is null.");

                        Console.WriteLine($"Received:\n{messagePayload}\n\n");

                        await Task.Delay(1000, stoppingToken);

                        await channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
                    }
                    catch (OperationCanceledException)
                    {
                        await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: true);
                    }
                    catch (JsonException jsonEx)
                    {
                        Console.WriteLine($"[ERROR] Failed to deserialize message: {jsonEx.Message}");
                        await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Failed to process message: {ex.Message}");
                        await channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
                    }
                };

                await BasicConsumeAsync(_channel, consumer, queueName, stoppingToken);
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Error during consumer dispose: {ex.Message}");
            }
            finally
            {
                base.Dispose();
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

        private static async Task<QueueDeclareOk> QueueDeclareAsync(IChannel channel, CancellationToken stoppingToken)
        {
            return await channel.QueueDeclareAsync(
                queue: string.Empty,
                durable: false,
                exclusive: true,
                autoDelete: true,
                arguments: null,
                cancellationToken: stoppingToken);
        }

        private async Task QueueBindAsync(IChannel channel, string queueName, CancellationToken stoppingToken)
        {
            await channel.QueueBindAsync(
                queue: queueName,
                exchange: _rabbitMqSettings.ExchangeName,
                routingKey: string.Empty,
                cancellationToken: stoppingToken);
        }

        private static async Task BasicConsumeAsync(IChannel channel, IAsyncBasicConsumer consumer, string queueName, CancellationToken stoppingToken)
        {
            await channel.BasicConsumeAsync(
                queueName,
                autoAck: false,
                consumer,
                cancellationToken: stoppingToken);
        }
    }
}
