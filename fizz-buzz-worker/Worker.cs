namespace fizz_buzz_worker;
using Confluent.Kafka;
using NewRelic.Api.Agent;
using StackExchange.Redis;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConnectionMultiplexer _redis;
    private readonly string kafkaBroker = Environment.GetEnvironmentVariable("KAFKA_BROKER") ?? "localhost:29092";

    private const string KAFKA_INGEST_TOPIC = "raw-input";
    private const string KAFKA_OUTPUT_TOPIC = "processed-input";

    public Worker(ILogger<Worker> logger, IConnectionMultiplexer multiplexer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _redis = multiplexer ?? throw new ArgumentNullException(nameof(multiplexer));
    }

    [Trace]
    private string Process(int input)
    {
        if (input % 3 == 0 && input % 5 == 0)
        {
            return "fizz-buzz";
        }
        else if (input % 3 == 0)
        {
            return "fizz";
        }
        else if (input % 5 == 0)
        {
            return "buzz";
        }
        else
        {
            return input.ToString();
        }
    }

    [Transaction(Web = false)]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
        Console.WriteLine("Broker configured as: " + kafkaBroker);

        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaBroker,
            GroupId = "fizz-buzz-worker",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            SessionTimeoutMs = 6000,
            EnablePartitionEof = true,
            PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky
        };

        var redisDb = _redis.GetDatabase();
        using (var c = new ConsumerBuilder<string, int>(config)
                // Note: All handlers are called on the main .Consume thread.
                .SetErrorHandler((_, e) => Console.WriteLine($"Error: {e.Reason}"))
                .SetStatisticsHandler((_, json) => Console.WriteLine($"Statistics: {json}"))
                .SetPartitionsAssignedHandler((c, partitions) =>
                {
                    // Since a cooperative assignor (CooperativeSticky) has been configured, the
                    // partition assignment is incremental (adds partitions to any existing assignment).
                    Console.WriteLine(
                        "Partitions incrementally assigned: [" +
                        string.Join(',', partitions.Select(p => p.Partition.Value)) +
                        "], all: [" +
                        string.Join(',', c.Assignment.Concat(partitions).Select(p => p.Partition.Value)) +
                        "]");

                    // Possibly manually specify start offsets by returning a list of topic/partition/offsets
                    // to assign to, e.g.:
                    // return partitions.Select(tp => new TopicPartitionOffset(tp, externalOffsets[tp]));
                })
                .SetPartitionsRevokedHandler((c, partitions) =>
                {
                    // Since a cooperative assignor (CooperativeSticky) has been configured, the revoked
                    // assignment is incremental (may remove only some partitions of the current assignment).
                    var remaining = c.Assignment.Where(atp => partitions.Where(rtp => rtp.TopicPartition == atp).Count() == 0);
                    Console.WriteLine(
                        "Partitions incrementally revoked: [" +
                        string.Join(',', partitions.Select(p => p.Partition.Value)) +
                        "], remaining: [" +
                        string.Join(',', remaining.Select(p => p.Partition.Value)) +
                        "]");
                })
                .SetPartitionsLostHandler((c, partitions) =>
                {
                    // The lost partitions handler is called when the consumer detects that it has lost ownership
                    // of its assignment (fallen out of the group).
                    Console.WriteLine($"Partitions were lost: [{string.Join(", ", partitions)}]");
                })
                .Build())
        {
            c.Subscribe(KAFKA_INGEST_TOPIC);
            try
            {
                while (true)
                {
                    try
                    {
                        var consumeResult = c.Consume(stoppingToken);

                        if (consumeResult.IsPartitionEOF)
                        {
                            Console.WriteLine(
                                $"Reached end of topic {consumeResult.Topic}, partition {consumeResult.Partition}, offset {consumeResult.Offset}.");

                            continue;
                        }

                        var input = consumeResult.Message.Value;
                        var key = consumeResult.Message.Key;
                        var output = Process(input);
                        Console.WriteLine($"Input: {input}, Output: {output}");

                        // Store the result in Redis
                        Console.WriteLine($"Writing to Redis: {key}:result, {output}");
                        redisDb.StringSet($"{key}:result", output, TimeSpan.FromMinutes(60));

                        Console.WriteLine($"Writing to Redis: {key}:status, Processed");
                        redisDb.StringSet($"{key}:status", "Processed", TimeSpan.FromMinutes(60));
                    }
                    catch (ConsumeException e)
                    {
                        Console.WriteLine($"Consume error: {e.Error.Reason}");

                        // If the topic does not exist, wait a bit and try again
                        Thread.Sleep(1000);
                        continue;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Ensure the consumer leaves the group cleanly and final offsets are committed.
                Console.WriteLine("Closing consumer");
                c.Close();
            }
        }
    }
}
