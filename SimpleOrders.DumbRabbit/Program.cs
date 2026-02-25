using Microsoft.Extensions.Options;
using SimpleOrders.DumbRabbit;
using SimpleOrders.Shared;
using SimpleOrders.Shared.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton(p => p.GetRequiredService<IOptions<RabbitMqSettings>>().Value);
builder.Services.AddSingleton<RabbitService>();

builder.Services.AddHostedService<OrdersWorker>();
builder.Services.AddHostedService<HelloWorker>();
var host = builder.Build();
host.Run();