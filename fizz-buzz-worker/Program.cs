using fizz_buzz_worker;
using StackExchange.Redis;

var redisHost = Environment.GetEnvironmentVariable("REDIS_HOST") ?? "localhost";
var multiplexer = ConnectionMultiplexer.Connect($"{redisHost}:6379");

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        // Add redis connection
        services.AddSingleton<IConnectionMultiplexer>(multiplexer);
        services.AddHostedService<Worker>();
    })
    .Build();

host.Run();
