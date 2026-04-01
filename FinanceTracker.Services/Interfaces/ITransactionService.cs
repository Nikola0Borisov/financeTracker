using System;
using System.Collections.Generic;
using FinanceTracker.Data.Entities;

namespace FinanceTracker.Services.Interfaces
{
    public interface ITransactionService
    {
        /// <summary>
        /// Добавя нова транзакция.
        /// </summary>
        /// <param name="transaction">Транзакцията за добавяне</param>
        void AddTransaction(Transaction transaction);

        /// <summary>
        /// Актуализира съществуваща транзакция.
        /// </summary>
        /// <param name="transaction">Транзакцията с променените данни</param>
        void UpdateTransaction(Transaction transaction);

        /// <summary>
        /// Изтрива транзакция по идентификатор.
        /// </summary>
        /// <param name="id">Идентификатор на транзакцията</param>
        void DeleteTransaction(int id);

        /// <summary>
        /// Връща транзакция по идентификатор, включително категорията ѝ.
        /// </summary>
        /// <param name="id">Идентификатор на транзакцията</param>
        /// <returns>Транзакция или null, ако не е намерена</returns>
        Transaction GetTransactionById(int id);

        /// <summary>
        /// Връща всички транзакции, сортирани по дата (най-новите първи).
        /// </summary>
        /// <returns>Колекция от всички транзакции</returns>
        IEnumerable<Transaction> GetAllTransactions();

        /// <summary>
        /// Връща транзакции в зададен времеви диапазон.
        /// </summary>
        /// <param name="start">Начална дата</param>
        /// <param name="end">Крайна дата</param>
        /// <returns>Транзакции, попадащи в диапазона</returns>
        IEnumerable<Transaction> GetTransactionsByDateRange(DateTime start, DateTime end);

        /// <summary>
        /// Изчислява баланса (сума на всички транзакции) до определена дата.
        /// </summary>
        /// <param name="asOfDate">Крайна дата; ако е null, балансът е за всички транзакции</param>
        /// <returns>Баланс</returns>
        decimal GetBalance(DateTime? asOfDate = null);

        /// <summary>
        /// Връща суми на разходите, групирани по категория, за даден период.
        /// </summary>
        /// <param name="start">Начална дата</param>
        /// <param name="end">Крайна дата</param>
        /// <returns>Речник с имена на категории и суми на разходите</returns>
        Dictionary<string, decimal> GetExpensesByCategory(DateTime start, DateTime end);

        /// <summary>
        /// Връща суми на приходите, групирани по категория, за даден период.
        /// </summary>
        /// <param name="start">Начална дата</param>
        /// <param name="end">Крайна дата</param>
        /// <returns>Речник с имена на категории и суми на приходите</returns>
        Dictionary<string, decimal> GetIncomesByCategory(DateTime start, DateTime end);

        /// <summary>
        /// Експортира транзакциите в CSV файл.
        /// </summary>
        /// <param name="filePath">Път до файла, където да се запише CSV</param>
        /// <param name="transactions">Колекция от транзакции за експорт</param>
        void ExportToCsv(string filePath, IEnumerable<Transaction> transactions);
    }
}