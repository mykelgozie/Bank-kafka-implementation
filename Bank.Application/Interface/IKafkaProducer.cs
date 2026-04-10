
namespace Bank.Application.Interface
{
    public interface IKafkaProducer
    {
        Task SendAsync(string topic, string message);
    }
}
