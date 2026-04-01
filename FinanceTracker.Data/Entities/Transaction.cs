using System;

namespace FinanceTracker.Data.Entities
{
    public class Transaction
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime Date { get; set; }
        public string? Note { get; set; }  // nullable string

        public int CategoryId { get; set; }
        public Category Category { get; set; }
    }
}
