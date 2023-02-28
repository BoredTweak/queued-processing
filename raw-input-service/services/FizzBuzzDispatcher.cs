using Confluent.Kafka;
using NewRelic.Api.Agent;
using StackExchange.Redis;

/// <summary>
/// This class is responsible for dispatching the input to the Kafka topic.
/// </summary>
/// <inheritdoc cref="IDispatcher"/>
public class FizzBuzzDispatcher : IDispatcher
{
    private readonly IConnectionMultiplexer _redis;

    public FizzBuzzDispatcher(IConnectionMultiplexer redis)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
    }

    private const string KAFKA_TOPIC = "raw-input";

    [Trace]
    public async Task<Guid?> Dispatch(int input)
    {
        var kafkaBroker = Environment.GetEnvironmentVariable("KAFKA_BROKER") ?? "localhost:29092";
        Console.WriteLine("Broker configured as: " + kafkaBroker);

        var config = new ProducerConfig { BootstrapServers = kafkaBroker };
        using (var p = new ProducerBuilder<string, int>(config).Build())
        {
            try
            {
                var identifier = Guid.NewGuid();

                // Write a message to redis to indicate that the message has been dispatched
                var redisDb = _redis.GetDatabase();

                // Status key is the identifier:status and value is "Dispatched"
                var redisKey = $"{identifier.ToString()}:status";

                Console.WriteLine($"Writing to Redis: {redisKey}:status, Dispatched");
                await redisDb.StringSetAsync(redisKey, "Dispatched", TimeSpan.FromMinutes(60));

                // Write to kafka topic
                Console.WriteLine("Calling Produce");
                var dr = await p.ProduceAsync(KAFKA_TOPIC, new Message<string, int> { Key = identifier.ToString(), Value = input });

                Console.WriteLine($"Delivered '{dr.Value}' with key '{dr.Key}' to '{dr.TopicPartitionOffset}'");
                return identifier;
            }
            catch (ProduceException<Null, string> e)
            {
                Console.WriteLine($"Delivery failed: {e.Error.Reason}");
                return null;
            }
        }
    }

    [Trace]
    /// <summary>
    /// Returns the status of the input processing.
    /// </summary>
    /// <param name="identifier">The identifier of the input</param>
    /// <returns> 
    /// The status of the input processing
    /// if processed then return ResultStatus.Processed
    /// if dispatched then return ResultStatus.Dispatched
    /// if invalid then return ResultStatus.Invalid
    /// </returns>

    public async Task<string?> GetStatus(Guid identifier)
    {
        var redisDb = _redis.GetDatabase();
        var statusKey = $"{identifier.ToString()}:status";
        var result = await redisDb.StringGetAsync(statusKey);

        switch (result)
        {
            case "Dispatched":
                return ResultStatus.Dispatched;
            case "Processed":
                return ResultStatus.Processed;
            default:
                return ResultStatus.Invalid;
        }
    }

    [Trace]
    public async Task<string?> GetResult(Guid identifier)
    {
        var redisDb = _redis.GetDatabase();
        var resultKey = $"{identifier.ToString()}:result";
        var result = await redisDb.StringGetAsync(resultKey);
        return result.ToString();
    }
}