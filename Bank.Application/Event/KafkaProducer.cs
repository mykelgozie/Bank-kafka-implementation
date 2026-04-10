using Bank.Application.Interface;
using Confluent.Kafka;

namespace Bank.Application.Event
{
    public class KafkaProducer : IKafkaProducer
    {

        private readonly IProducer<Null, string> _producer;

        public KafkaProducer()
        {
            var config = new ProducerConfig
            {
                BootstrapServers = "localhost:9092"
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
