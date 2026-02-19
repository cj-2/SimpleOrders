using Microsoft.Extensions.Options;
using SimpleOrders.DumbRabbit;
using SimpleOrders.Shared;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqConfig>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton(p => p.GetRequiredService<IOptions<RabbitMqConfig>>().Value);

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();