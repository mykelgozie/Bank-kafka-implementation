using Bank.Application.Interface;
using Bank.Domain.Configs;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Bank.Application.Event
{
    public class KafkaProducer : IKafkaProducer
    {

        private readonly IProducer<Null, string> _producer;
        private KafkaSettings _kafkaSettings;

        public KafkaProducer(IOptions<KafkaSettings> kafkaOptions)
        {
            _kafkaSettings = kafkaOptions.Value;
            var config = new ProducerConfig
            {
                BootstrapServers = _kafkaSettings.BootstrapServers,
            };

            _producer = new ProducerBuilder<Null, string>(config).Build();
        }

        public async Task SendAsync(string topic, string message)
        {
            await _producer.ProduceAsync(topic, new Message<Null, string>
            {
                Value = message
            });
        }
    }
}
