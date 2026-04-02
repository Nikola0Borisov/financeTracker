using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using FinanceTracker.Data;
using FinanceTracker.Data.Entities;
using FinanceTracker.Services.Implementations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinanceTracker.Tests.Services
{
    /// <summary>
    /// Тестове за TransactionService.
    /// </summary>
    public class TransactionServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly TransactionService _service;
        private readonly SqliteConnection _sqliteConnection; // за тестове с изключения

        /// <summary>
        /// Инициализира тестовете с In‑Memory база за повечето тестове и SQLite in‑memory за тестовете с изключения.
        /// </summary>
        public TransactionServiceTests()
        {
            // Основна In‑Memory база (без изключения при конкурентност)
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

            // SQLite in‑memory база за тестове, изискващи реални изключения (напр. DbUpdateConcurrencyException)
            _sqliteConnection = new SqliteConnection("DataSource=:memory:");
            _sqliteConnection.Open();
        }

        /// <summary>Освобождава ресурсите.</summary>
        public void Dispose()
        {
            _context.Dispose();
            _sqliteConnection.Dispose();
        }

        // ---------- Съществуващи тестове (запазете ги) ----------
        [Fact]
        public void AddTransaction_ShouldIncreaseCount()
        {
            var transaction = new Transaction { Amount = 100, Date = DateTime.Today, CategoryId = 1, Note = "Тест" };
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
            var foodCategory = _context.Categories.Single(c => c.Name == "Храна");
            var transactions = new[]
            {
                new Transaction { Amount = -20, Date = DateTime.Today, CategoryId = foodCategory.Id },
                new Transaction { Amount = -15, Date = DateTime.Today, CategoryId = foodCategory.Id },
                new Transaction { Amount = -5, Date = DateTime.Today, CategoryId = foodCategory.Id }
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

        [Fact]
        public void GetTransactionById_ShouldReturnCorrectTransaction()
        {
            var transaction = new Transaction { Amount = 150, Date = DateTime.Today, CategoryId = 1, Note = "Тест" };
            _service.AddTransaction(transaction);
            int id = transaction.Id;
            var result = _service.GetTransactionById(id);
            Assert.NotNull(result);
            Assert.Equal(150, result.Amount);
            Assert.Equal("Тест", result.Note);
        }

        [Fact]
        public void GetTransactionById_WhenNotExists_ShouldReturnNull()
        {
            var result = _service.GetTransactionById(999);
            Assert.Null(result);
        }

        [Fact]
        public void UpdateTransaction_ShouldUpdateExistingTransaction()
        {
            var transaction = new Transaction { Amount = 100, Date = DateTime.Today, CategoryId = 1, Note = "Оригинал" };
            _service.AddTransaction(transaction);
            int id = transaction.Id;
            transaction.Amount = 200;
            transaction.Note = "Променен";
            _service.UpdateTransaction(transaction);
            var updated = _service.GetTransactionById(id);
            Assert.Equal(200, updated.Amount);
            Assert.Equal("Променен", updated.Note);
        }

        [Fact]
        public void GetTransactionsByDateRange_ShouldReturnOnlyTransactionsInRange()
        {
            _context.Transactions.AddRange(
                new Transaction { Amount = 100, Date = new DateTime(2025, 1, 10), CategoryId = 1 },
                new Transaction { Amount = 200, Date = new DateTime(2025, 1, 20), CategoryId = 1 },
                new Transaction { Amount = 300, Date = new DateTime(2025, 2, 1), CategoryId = 1 }
            );
            _context.SaveChanges();
            var result = _service.GetTransactionsByDateRange(new DateTime(2025, 1, 15), new DateTime(2025, 1, 25));
            Assert.Single(result);
            Assert.Equal(200, result.First().Amount);
        }

        [Fact]
        public void GetIncomesByCategory_ShouldReturnCorrectSums()
        {
            var start = new DateTime(2025, 1, 1);
            var end = new DateTime(2025, 1, 31);
            var salaryCategory = _context.Categories.Single(c => c.Name == "Заплата");
            _context.Transactions.AddRange(
                new Transaction { Amount = 1000, Date = new DateTime(2025, 1, 10), CategoryId = salaryCategory.Id },
                new Transaction { Amount = 500, Date = new DateTime(2025, 1, 20), CategoryId = salaryCategory.Id }
            );
            _context.SaveChanges();
            var incomes = _service.GetIncomesByCategory(start, end);
            Assert.Single(incomes);
            Assert.Equal(1500, incomes["Заплата"]);
        }

        [Fact]
        public void GetIncomesByCategory_WhenNoIncomes_ShouldReturnEmpty()
        {
            var start = new DateTime(2025, 1, 1);
            var end = new DateTime(2025, 1, 31);
            var incomes = _service.GetIncomesByCategory(start, end);
            Assert.Empty(incomes);
        }

        [Fact]
        public void ExportToCsv_ShouldCreateFileWithTransactions()
        {
            var transactions = new List<Transaction>
            {
                new Transaction { Amount = 100, Date = DateTime.Today, CategoryId = 1, Note = "Тест" }
            };
            var tempFile = Path.GetTempFileName();
            try
            {
                _service.ExportToCsv(tempFile, transactions);
                var lines = File.ReadAllLines(tempFile);
                Assert.True(lines.Length > 1);
                Assert.Contains("100", lines[1]);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void DeleteTransaction_WhenNotExists_ShouldNotThrow()
        {
            var exception = Record.Exception(() => _service.DeleteTransaction(999));
            Assert.Null(exception);
        }

        [Fact]
        public void GetBalance_WithDate_ShouldSumOnlyUntilThatDate()
        {
            _context.Transactions.AddRange(
                new Transaction { Amount = 100, Date = new DateTime(2025, 1, 10), CategoryId = 1 },
                new Transaction { Amount = 50, Date = new DateTime(2025, 1, 15), CategoryId = 1 },
                new Transaction { Amount = 30, Date = new DateTime(2025, 1, 20), CategoryId = 1 }
            );
            _context.SaveChanges();
            var balance = _service.GetBalance(new DateTime(2025, 1, 15));
            Assert.Equal(150, balance);
        }

        [Fact]
        public void GetTransactionsByDateRange_WhenNoTransactionsInRange_ShouldReturnEmpty()
        {
            _context.Transactions.AddRange(
                new Transaction { Amount = 100, Date = new DateTime(2025, 1, 10), CategoryId = 1 },
                new Transaction { Amount = 200, Date = new DateTime(2025, 1, 20), CategoryId = 1 }
            );
            _context.SaveChanges();
            var result = _service.GetTransactionsByDateRange(new DateTime(2025, 2, 1), new DateTime(2025, 2, 10));
            Assert.Empty(result);
        }

        [Fact]
        public void GetExpensesByCategory_WhenNoExpenses_ShouldReturnEmpty()
        {
            var salaryCategory = _context.Categories.Single(c => c.Name == "Заплата");
            _context.Transactions.AddRange(
                new Transaction { Amount = 1000, Date = DateTime.Today, CategoryId = salaryCategory.Id }
            );
            _context.SaveChanges();
            var expenses = _service.GetExpensesByCategory(DateTime.Today.AddDays(-10), DateTime.Today.AddDays(10));
            Assert.Empty(expenses);
        }

        // ========== НОВИ ТЕСТОВЕ ЗА ПОВИШАВАНЕ НА ПОКРИТИЕТО ==========

        /// <summary>Тества GetBalance при празна база данни – очаква се 0.</summary>
        [Fact]
        public void GetBalance_WhenNoTransactions_ReturnsZero()
        {
            // Използваме чист контекст без транзакции
            using var cleanContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
            var cleanService = new TransactionService(cleanContext);
            var balance = cleanService.GetBalance();
            Assert.Equal(0, balance);
        }

        /// <summary>Тества GetBalance с дата, преди която няма транзакции – очаква се 0.</summary>
        [Fact]
        public void GetBalance_WithDateBeforeAnyTransaction_ReturnsZero()
        {
            var date = new DateTime(2000, 1, 1);
            var balance = _service.GetBalance(date);
            Assert.Equal(0, balance);
        }

        /// <summary>Тества UpdateTransaction за несъществуваща транзакция – очаква се DbUpdateConcurrencyException.</summary>
        [Fact]
        public void UpdateTransaction_WhenNotExists_ThrowsConcurrencyException()
        {
            // Използваме SQLite in‑memory, защото In‑Memory доставчикът не хвърля ConcurrencyException
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_sqliteConnection)
                .Options;
            using var sqliteContext = new AppDbContext(options);
            sqliteContext.Database.EnsureCreated();
            sqliteContext.Categories.AddRange(
                new Category { Name = "Заплата", Type = "Income" },
                new Category { Name = "Храна", Type = "Expense" }
            );
            sqliteContext.SaveChanges();

            var service = new TransactionService(sqliteContext);
            var fakeTransaction = new Transaction { Id = 9999, Amount = 100, CategoryId = 1 };

            Assert.Throws<DbUpdateConcurrencyException>(() => service.UpdateTransaction(fakeTransaction));
        }

        /// <summary>Тества DeleteTransaction за транзакция, която не съществува – няма изключение (вече го има).</summary>
        [Fact]
        public void DeleteTransaction_WhenIdDoesNotExist_DoesNotThrow()
        {
            var exception = Record.Exception(() => _service.DeleteTransaction(99999));
            Assert.Null(exception);
        }

        /// <summary>Тества GetTransactionsByDateRange с null период (не се използва, но за покритие).</summary>
        [Fact]
        public void GetTransactionsByDateRange_WithInvalidDates_ReturnsEmpty()
        {
            // Методът не проверява дали start > end, но ако подадем обратен диапазон, няма да върне нищо
            var result = _service.GetTransactionsByDateRange(DateTime.Today, DateTime.Today.AddDays(-1));
            Assert.Empty(result);
        }

        /// <summary>Тества експорт в CSV, когато директорията не съществува – трябва да я създаде.</summary>
        [Fact]
        public void ExportToCsv_CreatesDirectoryIfNotExists()
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var filePath = Path.Combine(tempDir, "test.csv");
            try
            {
                var transactions = new List<Transaction>
                {
                    new Transaction { Amount = 50, CategoryId = 1 }
                };
                _service.ExportToCsv(filePath, transactions);
                Assert.True(File.Exists(filePath));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }
    }
}