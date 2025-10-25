using RabbitMq.Consumer.Messages;
using RabbitMq.Shared.Infrastracture;
using RabbitMq.Shared.Infrastracture.Abstraction;
using RabbitMq.Shared.Settings;

var builder = Host.CreateApplicationBuilder(args);
ConfigureServices(builder);

var host = builder.Build();
host.Run();

static void ConfigureServices(HostApplicationBuilder builder)
{
    builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection(RabbitMqSettings.SectionName));
    builder.Services.AddSingleton<IRabbitMqConnectionProvider, RabbitMqConnectionProvider>();
    builder.Services.AddHostedService<MessageConsumer>();
}