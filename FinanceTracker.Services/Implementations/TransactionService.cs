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

        public TransactionService(AppDbContext context)
        {
            _context = context;
        }
        public void AddTransaction(Transaction transaction)
        {
            _context.Transactions.Add(transaction);
            _context.SaveChanges();
        }

        public void UpdateTransaction(Transaction transaction)
        {
            _context.Transactions.Update(transaction);
            _context.SaveChanges();
        }

        public void DeleteTransaction(int id)
        {
            var transaction = _context.Transactions.Find(id);
            if (transaction != null)
            {
                _context.Transactions.Remove(transaction);
                _context.SaveChanges();
            }
        }

        public Transaction GetTransactionById(int id)
        {
            return _context.Transactions
                .Include(t => t.Category)
                .FirstOrDefault(t => t.Id == id);
        }

        public IEnumerable<Transaction> GetAllTransactions()
        {
            return _context.Transactions
                .Include(t => t.Category)
                .OrderByDescending(t => t.Date)
                .ToList();
        }

        public IEnumerable<Transaction> GetTransactionsByDateRange(DateTime start, DateTime end)
        {
            return _context.Transactions
                .Include(t => t.Category)
                .Where(t => t.Date >= start && t.Date <= end)
                .OrderByDescending(t => t.Date)
                .ToList();
        }

        public decimal GetBalance(DateTime? asOfDate = null)
        {
            var query = _context.Transactions.AsQueryable();
            if (asOfDate.HasValue)
                query = query.Where(t => t.Date <= asOfDate.Value);
            return query.Sum(t => t.Amount);
        }
        public Dictionary<string, decimal> GetExpensesByCategory(DateTime start, DateTime end)
        {
            var query = from t in _context.Transactions
                        join c in _context.Categories on t.CategoryId equals c.Id
                        where t.Date >= start && t.Date <= end && t.Amount < 0
                        group t by c.Name into g
                        select new { Category = g.Key, Total = -g.Sum(t => t.Amount) };
            return query.ToDictionary(x => x.Category, x => x.Total);
        }

        public Dictionary<string, decimal> GetIncomesByCategory(DateTime start, DateTime end)
        {
            var query = from t in _context.Transactions
                        join c in _context.Categories on t.CategoryId equals c.Id
                        where t.Date >= start && t.Date <= end && t.Amount > 0
                        group t by c.Name into g
                        select new { Category = g.Key, Total = g.Sum(t => t.Amount) };
            return query.ToDictionary(x => x.Category, x => x.Total);
        }
        public void ExportToCsv(string filePath, IEnumerable<Transaction> transactions)
        {
            using var writer = new StreamWriter(filePath);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(transactions);
        }
    }
}