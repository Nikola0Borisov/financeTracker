
using System;
using System.Collections.Generic;
using FinanceTracker.Data.Entities;

namespace FinanceTracker.Services.Interfaces
{
    public interface ITransactionService
    {
        void AddTransaction(Transaction transaction);
        void UpdateTransaction(Transaction transaction);
        void DeleteTransaction(int id);
        Transaction GetTransactionById(int id);
        IEnumerable<Transaction> GetAllTransactions();
        IEnumerable<Transaction> GetTransactionsByDateRange(DateTime start, DateTime end);
        decimal GetBalance(DateTime? asOfDate = null);
        Dictionary<string, decimal> GetExpensesByCategory(DateTime start, DateTime end);
        Dictionary<string, decimal> GetIncomesByCategory(DateTime start, DateTime end);
        void ExportToCsv(string filePath, IEnumerable<Transaction> transactions);
    }
}