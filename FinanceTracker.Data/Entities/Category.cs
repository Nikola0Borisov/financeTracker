using System.Collections.Generic;

namespace FinanceTracker.Data.Entities
{
    /// <summary>
    /// Представлява категория за приходи или разходи.
    /// </summary>
    public class Category
    {
        /// <summary>
        /// Уникален идентификатор на категорията.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Име на категорията (напр. "Храна", "Заплата").
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Тип на категорията – "Income" (приход) или "Expense" (разход).
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Навигационно свойство – списък на транзакциите, принадлежащи към тази категория.
        /// </summary>
        public ICollection<Transaction> Transactions { get; set; }
    }
}