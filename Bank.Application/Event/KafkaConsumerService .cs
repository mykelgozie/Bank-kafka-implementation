using Bank.Application.Interface;
using Bank.Domain.Dtos.Request;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

namespace Bank.Application.Event
{
    public class KafkaConsumerService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public KafkaConsumerService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Task.Run(() => StartConsumerLoop(stoppingToken), stoppingToken);
            return Task.CompletedTask;   
        }

        private void StartConsumerLoop(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = "localhost:9092",
                GroupId = "transaction-group-v2",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                EnableAutoOffsetStore = false,

                SessionTimeoutMs = 10000,
                HeartbeatIntervalMs = 3000,
                ClientId = "webhook-consumer"
            };

            var consumer = new ConsumerBuilder<Ignore, string>(config)
                .SetErrorHandler((_, e) =>
                {
                    Console.WriteLine($"Kafka Error: {e.Reason}");
                })
                .Build();

            consumer.Subscribe("tran-topic");

            Console.WriteLine("Kafka Consumer started...");

            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        var result = consumer.Consume(TimeSpan.FromSeconds(1));

                        if (result?.Message?.Value == null)
                            continue;

                        var transactionEvent =
                            JsonSerializer.Deserialize<TransactionEvent>(result.Message.Value);

                        if (transactionEvent == null)
                        {
                            consumer.Commit(result);
                            continue;
                        }

                        using var scope = _scopeFactory.CreateScope();
                        var webhookService = scope.ServiceProvider
                            .GetRequiredService<IProcessWebHookService>();

                        webhookService.ProcessTransactionEvent(transactionEvent)
                            .GetAwaiter().GetResult();

                        consumer.Commit(result);
                    }
                    catch (ConsumeException ex)
                    {
                        Console.WriteLine($"Consume error: {ex.Error.Reason}");
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Processing error: {ex.Message}");
                    }
                }
            }
            finally
            {
                consumer.Close(); // important: commits final offsets & leaves group cleanly
            }
        }
    }
}
