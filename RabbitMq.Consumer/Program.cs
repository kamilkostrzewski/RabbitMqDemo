using RabbitMq.Consumer.Messages;
using RabbitMq.Shared.Settings;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection(RabbitMqSettings.SectionName)
);
builder.Services.AddHostedService<MessageConsumer>();

var host = builder.Build();
host.Run();