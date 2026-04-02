using System;
using System.Collections.Generic;
using FinanceTracker.Data.Entities;

namespace FinanceTracker.Services.Interfaces
{
    /// <summary>
    /// Дефинира операциите за управление на финансови транзакции.
    /// </summary>
    public interface ITransactionService
    {
        /// <summary>Добавя нова транзакция.</summary>
        /// <param name="transaction">Транзакцията за добавяне.</param>
        void AddTransaction(Transaction transaction);

        /// <summary>Актуализира съществуваща транзакция.</summary>
        /// <param name="transaction">Транзакцията с новите данни.</param>
        void UpdateTransaction(Transaction transaction);

        /// <summary>Изтрива транзакция по идентификатор.</summary>
        /// <param name="id">Идентификатор на транзакцията.</param>
        void DeleteTransaction(int id);

        /// <summary>Връща транзакция по идентификатор заедно с нейната категория.</summary>
        /// <param name="id">Идентификатор на транзакцията.</param>
        Transaction GetTransactionById(int id);

        /// <summary>Връща всички транзакции, сортирани по дата (най-новите първи).</summary>
        IEnumerable<Transaction> GetAllTransactions();

        /// <summary>Връща транзакциите в даден времеви диапазон.</summary>
        /// <param name="start">Начална дата.</param>
        /// <param name="end">Крайна дата.</param>
        IEnumerable<Transaction> GetTransactionsByDateRange(DateTime start, DateTime end);

        /// <summary>Изчислява баланса (сума на всички транзакции) до определена дата.</summary>
        /// <param name="asOfDate">Крайна дата; ако е null, балансът е за всички транзакции.</param>
        decimal GetBalance(DateTime? asOfDate = null);

        /// <summary>Връща сумите на разходите, групирани по категория, за даден период.</summary>
        /// <param name="start">Начална дата.</param>
        /// <param name="end">Крайна дата.</param>
        Dictionary<string, decimal> GetExpensesByCategory(DateTime start, DateTime end);

        /// <summary>Връща сумите на приходите, групирани по категория, за даден период.</summary>
        /// <param name="start">Начална дата.</param>
        /// <param name="end">Крайна дата.</param>
        Dictionary<string, decimal> GetIncomesByCategory(DateTime start, DateTime end);

        /// <summary>Експортира транзакциите в CSV файл.</summary>
        /// <param name="filePath">Пълен път до CSV файла.</param>
        /// <param name="transactions">Колекция от транзакции за експорт.</param>
        void ExportToCsv(string filePath, IEnumerable<Transaction> transactions);
    }
}