using Bank.Domain.Enum;
using System.ComponentModel.DataAnnotations;

namespace Bank.Domain.Entities
{
    public class Transaction : BaseEntity
    {
        public string TransactionId { get; set; }
        public decimal Amount { get; set; }

        public decimal NetAmount { get; set; }
        public decimal Fee { get; set; }
        public string CurrencyCode { get; set; }
        public TransactionStatus Status { get; set; }
        public string Reference { get; set; }
        public string Payload { get; set; }
    }
}
