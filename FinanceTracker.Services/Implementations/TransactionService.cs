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
    /// <summary>
    /// DTO за експорт на транзакции в CSV – избягва навигационните свойства.
    /// </summary>
    public class TransactionExportDto
    {
        /// <summary>Идентификатор на транзакцията.</summary>
        public int Id { get; set; }
        /// <summary>Дата на транзакцията.</summary>
        public DateTime Date { get; set; }
        /// <summary>Сума на транзакцията.</summary>
        public decimal Amount { get; set; }
        /// <summary>Име на категорията.</summary>
        public string CategoryName { get; set; }
        /// <summary>Бележка към транзакцията.</summary>
        public string Note { get; set; }
    }

    /// <summary>
    /// Реализация на услугата за управление на транзакции.
    /// </summary>
    public class TransactionService : ITransactionService
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Конструктор, приемащ контекст на базата данни.
        /// </summary>
        /// <param name="context">Контекст на базата данни.</param>
        public TransactionService(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public void AddTransaction(Transaction transaction)
        {
            _context.Transactions.Add(transaction);
            _context.SaveChanges();
        }

        /// <inheritdoc />
        public void UpdateTransaction(Transaction transaction)
        {
            _context.Transactions.Update(transaction);
            _context.SaveChanges();
        }

        /// <inheritdoc />
        public void DeleteTransaction(int id)
        {
            var transaction = _context.Transactions.Find(id);
            if (transaction != null)
            {
                _context.Transactions.Remove(transaction);
                _context.SaveChanges();
            }
        }

        /// <inheritdoc />
        public Transaction GetTransactionById(int id)
        {
            return _context.Transactions
                .Include(t => t.Category)
                .FirstOrDefault(t => t.Id == id);
        }

        /// <inheritdoc />
        public IEnumerable<Transaction> GetAllTransactions()
        {
            return _context.Transactions
                .Include(t => t.Category)
                .OrderByDescending(t => t.Date)
                .ToList();
        }

        /// <inheritdoc />
        public IEnumerable<Transaction> GetTransactionsByDateRange(DateTime start, DateTime end)
        {
            return _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Date >= start && t.Date <= end)
                .OrderByDescending(t => t.Date)
                .ToList();
        }

        /// <inheritdoc />
        public decimal GetBalance(DateTime? asOfDate = null)
        {
            var query = _context.Transactions.AsQueryable();
            if (asOfDate.HasValue)
                query = query.Where(t => t.Date <= asOfDate.Value);
            // Изтегляме сумите в паметта и сумираме, за да избегнем проблем със SQLite
            return query.Select(t => t.Amount).ToList().Sum();
        }

        /// <inheritdoc />
        public Dictionary<string, decimal> GetExpensesByCategory(DateTime start, DateTime end)
        {
            // Изтегляме транзакциите в паметта и групираме на клиента (SQLite не поддържа агрегация на decimal)
            var transactionsInPeriod = _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Date >= start && t.Date <= end && t.Amount < 0)
                .ToList();

            return transactionsInPeriod
                .GroupBy(t => t.Category.Name)
                .ToDictionary(g => g.Key, g => -g.Sum(t => t.Amount));
        }

        /// <inheritdoc />
        public Dictionary<string, decimal> GetIncomesByCategory(DateTime start, DateTime end)
        {
            var transactionsInPeriod = _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Date >= start && t.Date <= end && t.Amount > 0)
                .ToList();

            return transactionsInPeriod
                .GroupBy(t => t.Category.Name)
                .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
        }

        /// <inheritdoc />
        public void ExportToCsv(string filePath, IEnumerable<Transaction> transactions)
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var dtoList = transactions.Select(t => new TransactionExportDto
            {
                Id = t.Id,
                Date = t.Date,
                Amount = t.Amount,
                CategoryName = t.Category?.Name ?? string.Empty,
                Note = t.Note ?? string.Empty
            }).ToList();

            using var writer = new StreamWriter(filePath);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(dtoList);
        }
    }
}