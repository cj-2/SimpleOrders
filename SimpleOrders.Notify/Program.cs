using Microsoft.Extensions.Options;
using SimpleOrders.Notify;
using SimpleOrders.Shared;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection("Kafka"));
builder.Services.AddSingleton(p => p.GetRequiredService<IOptions<KafkaSettings>>().Value);

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();