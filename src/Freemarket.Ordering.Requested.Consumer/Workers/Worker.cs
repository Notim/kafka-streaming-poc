using Confluent.Kafka;
using Freemarket.Ordering.Applications.ExternalEvents;
using Freemarket.Ordering.Infrastructure.Kafka;

namespace OrderRequestConsumer.Workers;

public class Worker : BackgroundService
{

    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        
        var config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = GroupIds.MailingService,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };
        
        using (var consumer = new ConsumerBuilder<string, OrderRequestedByCustomer>(config)
               .SetKeyDeserializer(Deserializers.Utf8)
               .SetValueDeserializer(new XmlKakfaMessage<OrderRequestedByCustomer>())
               .SetErrorHandler((_, e) => _logger.LogCritical($"Error: {e.Reason}"))
               .Build()
        )
        {
            
            consumer.Subscribe(Topics.OrderRequestedTopic);

            while (!stoppingToken.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(stoppingToken);
                
                if (consumeResult is null)
                    continue;
                
                _logger.LogInformation("Message consumed {Topic} {Offset} {Partition} {Key} {@Value}", consumeResult.Topic, consumeResult.Offset, consumeResult.Partition.Value, consumeResult.Message.Key, consumeResult.Message.Value.ToString());
                
                Thread.Sleep(1000);
                
                try
                {
                    consumer.Commit(consumeResult);
                }
                catch (KafkaException e)
                {
                    Console.WriteLine($"Commit error: {e.Error.Reason}");
                }
            }
            
            consumer.Close();
        }
    }

}