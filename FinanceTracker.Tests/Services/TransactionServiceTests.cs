using System;
using System.Linq;
using FinanceTracker.Data;
using FinanceTracker.Data.Entities;
using FinanceTracker.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinanceTracker.Tests.Services
{
    public class TransactionServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly TransactionService _service;

        public TransactionServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);
            _service = new TransactionService(_context);

            _context.Categories.AddRange(
                new Category { Name = "Заплата", Type = "Income" },
                new Category { Name = "Храна", Type = "Expense" }
            );
            _context.SaveChanges();
        }

        [Fact]
        public void AddTransaction_ShouldIncreaseCount()
        {
            var transaction = new Transaction
            {
                Amount = 100,
                Date = DateTime.Today,
                CategoryId = 1,
                Note = "Тест"
            };

            _service.AddTransaction(transaction);

            Assert.Equal(1, _context.Transactions.Count());
        }

        [Fact]
        public void GetBalance_ShouldSumAllTransactions()
        {
            _context.Transactions.AddRange(
                new Transaction { Amount = 100, CategoryId = 1 },
                new Transaction { Amount = -30, CategoryId = 2 }
            );
            _context.SaveChanges();

            var balance = _service.GetBalance();

            Assert.Equal(70, balance);
        }

        [Fact]
        public void GetExpensesByCategory_ShouldReturnCorrectSums()
        {
            var start = DateTime.Today.AddDays(-10);
            var end = DateTime.Today;

            // Вземаме категорията "Храна" от контекста
            var foodCategory = _context.Categories.Single(c => c.Name == "Храна");

            var transactions = new[]
            {
                new Transaction
                {
                    Amount = -20,
                    Date = DateTime.Today,
                    CategoryId = foodCategory.Id,
                    Category = foodCategory  // задаваме навигационното свойство, за да избегнем null
                },
                new Transaction
                {
                    Amount = -15,
                    Date = DateTime.Today,
                    CategoryId = foodCategory.Id,
                    Category = foodCategory
                },
                new Transaction
                {
                    Amount = -5,
                    Date = DateTime.Today,
                    CategoryId = foodCategory.Id,
                    Category = foodCategory
                }
            };
            _context.Transactions.AddRange(transactions);
            _context.SaveChanges();

            var expenses = _service.GetExpensesByCategory(start, end);

            Assert.Single(expenses);
            Assert.Equal(40, expenses["Храна"]);
        }

        [Fact]
        public void DeleteTransaction_ShouldRemoveTransaction()
        {
            var transaction = new Transaction { Amount = 50, CategoryId = 1 };
            _context.Transactions.Add(transaction);
            _context.SaveChanges();
            int id = transaction.Id;

            _service.DeleteTransaction(id);

            Assert.Null(_context.Transactions.Find(id));
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}