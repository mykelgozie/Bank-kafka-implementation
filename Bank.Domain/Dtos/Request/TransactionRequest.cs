namespace Bank.Domain.Dtos.Request
{
    public class TransactionRequest
    {
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public string CurrencyCode { get; set; }
        public int Status { get; set; }
        public string Payload { get; set; }
    }
}
