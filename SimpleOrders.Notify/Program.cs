using Microsoft.Extensions.Options;
using SimpleOrders.Notify;
using SimpleOrders.Shared;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<KafkaConfig>(builder.Configuration.GetSection("Kafka"));
builder.Services.AddSingleton(p => p.GetRequiredService<IOptions<KafkaConfig>>().Value);

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();