using System.Numerics;
using System.Text.Json.Nodes;
using Confluent.Kafka;

namespace KafkaDebeziumConsumer;

public class Program
{
    public static void Main(string[] args)
    {
        var consumerService = new KafkaConsumerService();
        consumerService.Start();
    }
}

public class KafkaConsumerService
{
    private const string Topic = "pgdemo.public.products";

    private readonly ConsumerConfig _config = new()
    {
        BootstrapServers = "kafka:9092",
        GroupId = "debezium-net-consumer",
        AutoOffsetReset = AutoOffsetReset.Earliest, // Use "Latest" for latest messages
        EnableAutoCommit = false,
        MaxPollIntervalMs = 300000,
        SessionTimeoutMs = 10000,
    };

    public void Start()
    {
        using var consumer = new ConsumerBuilder<Ignore, string>(_config).Build();
        consumer.Subscribe(Topic);

        Console.WriteLine($"Listening to topic: {Topic}");
        Console.WriteLine("Press Ctrl+C to exit.\n");

        try
        {
            while (true)
            {
                var result = consumer.Consume(CancellationToken.None);
                bool success = ProcessWithRetry(result, maxRetries: 3);

                if (success)
                {
                    consumer.Commit(result);
                }
                else
                {
                    Console.WriteLine($"❌ Giving up after retries for message at {result.TopicPartitionOffset}");
                    consumer.Commit(result); // Still commit to skip the message
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Closing consumer.");
        }
    }

    private bool ProcessWithRetry(ConsumeResult<Ignore, string> result, int maxRetries)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                ProcessMessage(result.Message.Value, result.TopicPartitionOffset);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error on attempt {attempt}/{maxRetries}: {ex.Message}");
            }
        }

        return false;
    }

    private void ProcessMessage(string message, TopicPartitionOffset offset)
    {
        // If message is null or empty then skip it
        if (string.IsNullOrEmpty(message))
        {
            Console.WriteLine("Message is null or empty, skipping.");
            return;
        }

        Console.WriteLine($"Message at {offset}:");
        Console.WriteLine(message);
        Console.WriteLine(new string('-', 80));

        var json = JsonNode.Parse(message);
        var payload = json?["payload"];
        var source = payload?["source"];
        var snapshot = source?["snapshot"]?.ToString() ?? "false";
        var op = payload?["op"]?.ToString();

        // Tag snapshot vs live
        var tag = (snapshot == "true" || snapshot == "last") ? "📦 SNAPSHOT" : "⚡ LIVE";
        Console.WriteLine($"[{tag}] op={op}");

        JsonNode? after = payload?["after"];
        JsonNode? before = payload?["before"];

        if (after != null)
        {
            int id = after?["id"]?.GetValue<int>() ?? 0;
            string name = after?["name"]?.GetValue<string>() ?? "";
            int scale = after?["price"]?["scale"]?.GetValue<int>() ?? 0;
            string? value = after?["price"]?["value"]?.ToString();
            decimal price = value != null ? DecodeDecimal(value, scale) : 0;

            if (name.Contains("fail", StringComparison.OrdinalIgnoreCase))
                throw new Exception($"Simulated failure due to name '{name}'");

            if (op == "u" && after != null && before != null)
            {
                Console.WriteLine($"Updated id={id}, name={name}, price={price}");
                PrintDiff(before, after);
            }
            else if (op == "c" && after != null)
            {
                Console.WriteLine($"Created id={id}, name={name}, price={price}");
            }
            else if (op == "r" && after != null)
            {
                Console.WriteLine($"Read id={id}, name={name}, price={price}");
            }
            else
            {
                Console.WriteLine($"Unhandled op={op}");
            }
        }
        else if (op == "d")
        {
            if (before != null)
            {
                int id = before?["id"]?.GetValue<int>() ?? 0;
                Console.WriteLine($"Deleted id={id}");
            }
            else
            {
                Console.WriteLine($"Unhandled deleted op={op}");
            }
        }

        Console.WriteLine(new string('-', 80));
    }

    private decimal DecodeDecimal(string base64, int scale)
    {
        try
        {
            var bytes = Convert.FromBase64String(base64);
            var bigInt = new BigInteger(bytes.Reverse().ToArray());
            return (decimal)bigInt / (decimal)Math.Pow(10, scale);
        }
        catch
        {
            Console.WriteLine($"Error decoding decimal: {base64}, returning 0");
            return 0;
        }
    }
    
    private void PrintDiff(JsonNode? before, JsonNode? after)
    {
        if (before is null || after is null) return;

        foreach (var prop in before.AsObject())
        {
            string key = prop.Key;
            var beforeVal = prop.Value?.ToJsonString();
            var afterVal  = after[key]?.ToJsonString();

            if (beforeVal != afterVal)
                Console.WriteLine($"  • {key}: {beforeVal}  →  {afterVal}");
        }
    }
}
