
namespace Bank.Domain.Configs
{
    public class KafkaSettings
    {
        public string BootstrapServers { get; set; }
        public string ConsumerGroupId { get; set; }
        public string Topic { get; set; }
    }
}
