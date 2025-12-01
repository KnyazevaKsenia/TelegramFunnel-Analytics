using CommonMongoModels;
using CommonRabbitMq;
using ReportWorker;


var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<Worker>();

builder.Services.Configure<RabbitMqSettings>(
    builder.Configuration.GetSection("RabbitMQ"));

builder.Services.AddSingleton<MongoDbContext>(); 

var host = builder.Build();
host.Run();
