using System;

namespace FinanceTracker.Data.Entities
{
    /// <summary>
    /// Представлява финансова транзакция – приход или разход.
    /// </summary>
    public class Transaction
    {
        /// <summary>
        /// Уникален идентификатор на транзакцията.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Сума на транзакцията (положителна за приход, отрицателна за разход).
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Дата на извършване на транзакцията.
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Незадължителна бележка (максимум 50 символа).
        /// </summary>
        public string? Note { get; set; }

        /// <summary>
        /// Идентификатор на категорията, към която принадлежи транзакцията.
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// Навигационно свойство – категорията на транзакцията.
        /// </summary>
        public Category Category { get; set; }
    }
}