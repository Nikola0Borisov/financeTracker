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
        [Fact]
        public void GetTransactionById_ShouldReturnCorrectTransaction()
        {
            var transaction = new Transaction
            {
                Amount = 150,
                Date = DateTime.Today,
                CategoryId = 1,
                Note = "Тест"
            };
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
            var transaction = new Transaction
            {
                Amount = 100,
                Date = DateTime.Today,
                CategoryId = 1,
                Note = "Оригинал"
            };
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
                new Transaction { Amount = 1000, Date = new DateTime(2025, 1, 10), CategoryId = salaryCategory.Id, Category = salaryCategory },
                new Transaction { Amount = 500, Date = new DateTime(2025, 1, 20), CategoryId = salaryCategory.Id, Category = salaryCategory }
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
                Assert.True(lines.Length > 1); // заглавен ред + данни
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
            _service.DeleteTransaction(999);
            // Ако стигне дотук без изключение, тестът е успешен
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
            Assert.Equal(150, balance); // 100 + 50
        }
        [Fact]
        public void UpdateTransaction_WhenNotExists_ShouldThrowConcurrencyException()
        {
            var nonExistent = new Transaction { Id = 999, Amount = 100, CategoryId = 1 };
            Assert.Throws<Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException>(
                () => _service.UpdateTransaction(nonExistent));
        }
        [Fact]
        public void GetTransactionsByDateRange_WhenNoTransactionsInRange_ShouldReturnEmpty()
        {
            // Arrange
            _context.Transactions.AddRange(
                new Transaction { Amount = 100, Date = new DateTime(2025, 1, 10), CategoryId = 1 },
                new Transaction { Amount = 200, Date = new DateTime(2025, 1, 20), CategoryId = 1 }
            );
            _context.SaveChanges();

            // Act
            var result = _service.GetTransactionsByDateRange(new DateTime(2025, 2, 1), new DateTime(2025, 2, 10));

            // Assert
            Assert.Empty(result);
        }
        [Fact]
        public void GetExpensesByCategory_WhenNoExpenses_ShouldReturnEmpty()
        {
            // Arrange – добавяме само приходи
            var salaryCategory = _context.Categories.Single(c => c.Name == "Заплата");
            _context.Transactions.AddRange(
                new Transaction { Amount = 1000, Date = DateTime.Today, CategoryId = salaryCategory.Id, Category = salaryCategory }
            );
            _context.SaveChanges();

            // Act
            var expenses = _service.GetExpensesByCategory(DateTime.Today.AddDays(-10), DateTime.Today.AddDays(10));

            // Assert
            Assert.Empty(expenses);
        }
        [Fact]
        public void DefaultDbContext_ShouldWorkWithSqlite()
        {
            // Създаваме контекст без опции – ще използва SQLite файл
            using var context = new AppDbContext();
            // Изтриваме файла, ако съществува, и създаваме наново
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            context.Categories.Add(new Category { Name = "SQLiteTest", Type = "Income" });
            context.SaveChanges();

            var categories = context.Categories.ToList();
            Assert.Single(categories);
            Assert.Equal("SQLiteTest", categories[0].Name);

            // Почистваме
            context.Database.EnsureDeleted();
        }

    }
}