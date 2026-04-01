using System.Collections.Generic;

namespace FinanceTracker.Data.Entities
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // "Income" или "Expense"

        public ICollection<Transaction> Transactions { get; set; }
    }
}