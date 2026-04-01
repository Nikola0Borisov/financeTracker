using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using CsvHelper;
using FinanceTracker.Data;
using FinanceTracker.Data.Entities;
using FinanceTracker.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Services.Implementations
{
    public class TransactionService : ITransactionService
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Създава нова инстанция на услугата за транзакции.
        /// </summary>
        /// <param name="context">Контекст на базата данни</param>
        public TransactionService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Добавя нова транзакция.
        /// </summary>
        /// <param name="transaction">Транзакцията за добавяне</param>
        public void AddTransaction(Transaction transaction)
        {
            _context.Transactions.Add(transaction);
            _context.SaveChanges();
        }

        /// <summary>
        /// Актуализира съществуваща транзакция.
        /// </summary>
        /// <param name="transaction">Транзакцията с променените данни</param>
        public void UpdateTransaction(Transaction transaction)
        {
            _context.Transactions.Update(transaction);
            _context.SaveChanges();
        }

        /// <summary>
        /// Изтрива транзакция по идентификатор.
        /// </summary>
        /// <param name="id">Идентификатор на транзакцията</param>
        public void DeleteTransaction(int id)
        {
            var transaction = _context.Transactions.Find(id);
            if (transaction != null)
            {
                _context.Transactions.Remove(transaction);
                _context.SaveChanges();
            }
        }

        /// <summary>
        /// Връща транзакция по идентификатор, включително категорията ѝ.
        /// </summary>
        /// <param name="id">Идентификатор на транзакцията</param>
        /// <returns>Транзакция или null, ако не е намерена</returns>
        public Transaction GetTransactionById(int id)
        {
            return _context.Transactions
                .Include(t => t.Category)
                .FirstOrDefault(t => t.Id == id);
        }

        /// <summary>
        /// Връща всички транзакции, сортирани по дата (най-новите първи).
        /// </summary>
        /// <returns>Колекция от всички транзакции</returns>
        public IEnumerable<Transaction> GetAllTransactions()
        {
            return _context.Transactions
                .Include(t => t.Category)
                .OrderByDescending(t => t.Date)
                .ToList();
        }

        /// <summary>
        /// Връща транзакции в зададен времеви диапазон.
        /// </summary>
        /// <param name="start">Начална дата</param>
        /// <param name="end">Крайна дата</param>
        /// <returns>Транзакции, попадащи в диапазона</returns>
        public IEnumerable<Transaction> GetTransactionsByDateRange(DateTime start, DateTime end)
        {
            return _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Date >= start && t.Date <= end)
                .OrderByDescending(t => t.Date)
                .ToList();
        }

        /// <summary>
        /// Изчислява баланса (сума на всички транзакции) до определена дата.
        /// </summary>
        /// <param name="asOfDate">Крайна дата; ако е null, балансът е за всички транзакции</param>
        /// <returns>Баланс</returns>
        public decimal GetBalance(DateTime? asOfDate = null)
        {
            var query = _context.Transactions.AsQueryable();
            if (asOfDate.HasValue)
                query = query.Where(t => t.Date <= asOfDate.Value);
            return query.Sum(t => t.Amount);
        }

        /// <summary>
        /// Връща суми на разходите, групирани по категория, за даден период.
        /// </summary>
        /// <param name="start">Начална дата</param>
        /// <param name="end">Крайна дата</param>
        /// <returns>Речник с имена на категории и суми на разходите</returns>
        public Dictionary<string, decimal> GetExpensesByCategory(DateTime start, DateTime end)
        {
            var query = from t in _context.Transactions
                        join c in _context.Categories on t.CategoryId equals c.Id
                        where t.Date >= start && t.Date <= end && t.Amount < 0
                        group t by c.Name into g
                        select new { Category = g.Key, Total = -g.Sum(t => t.Amount) };
            return query.ToDictionary(x => x.Category, x => x.Total);
        }

        /// <summary>
        /// Връща суми на приходите, групирани по категория, за даден период.
        /// </summary>
        /// <param name="start">Начална дата</param>
        /// <param name="end">Крайна дата</param>
        /// <returns>Речник с имена на категории и суми на приходите</returns>
        public Dictionary<string, decimal> GetIncomesByCategory(DateTime start, DateTime end)
        {
            var query = from t in _context.Transactions
                        join c in _context.Categories on t.CategoryId equals c.Id
                        where t.Date >= start && t.Date <= end && t.Amount > 0
                        group t by c.Name into g
                        select new { Category = g.Key, Total = g.Sum(t => t.Amount) };
            return query.ToDictionary(x => x.Category, x => x.Total);
        }

        /// <summary>
        /// Експортира транзакциите в CSV файл.
        /// </summary>
        /// <param name="filePath">Път до файла, където да се запише CSV</param>
        /// <param name="transactions">Колекция от транзакции за експорт</param>
        public void ExportToCsv(string filePath, IEnumerable<Transaction> transactions)
        {
            using var writer = new StreamWriter(filePath);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(transactions);
        }
    }
}