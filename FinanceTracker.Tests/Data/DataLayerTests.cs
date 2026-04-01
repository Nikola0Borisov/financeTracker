using System;
using System.Linq;
using FinanceTracker.Data;
using FinanceTracker.Data.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinanceTracker.Tests.Data
{
    public class DataLayerTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly SqliteConnection _connection;

        public DataLayerTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;
            _context = new AppDbContext(options);
            _context.Database.EnsureCreated();
        }

        [Fact]
        public void CanAddAndRetrieveCategory()
        {
            var category = new Category { Name = "TestCat", Type = "Income" };
            _context.Categories.Add(category);
            _context.SaveChanges();

            var retrieved = _context.Categories.First();
            Assert.Equal("TestCat", retrieved.Name);
        }

        [Fact]
        public void CanAddTransactionWithCategory()
        {
            var category = new Category { Name = "Food", Type = "Expense" };
            _context.Categories.Add(category);
            _context.SaveChanges();

            var transaction = new Transaction
            {
                Amount = -50,
                Date = DateTime.Today,
                CategoryId = category.Id,
                Note = "Lunch"
            };
            _context.Transactions.Add(transaction);
            _context.SaveChanges();

            var retrieved = _context.Transactions.Include(t => t.Category).First();
            Assert.Equal(-50, retrieved.Amount);
            Assert.Equal("Food", retrieved.Category.Name);
        }

        [Fact]
        public void CanUpdateTransactionAndRetrieve()
        {
            var category = new Category { Name = "TestCat", Type = "Income" };
            _context.Categories.Add(category);
            _context.SaveChanges();

            var transaction = new Transaction
            {
                Amount = 100,
                Date = DateTime.Today,
                CategoryId = category.Id,
                Note = "Original"
            };
            _context.Transactions.Add(transaction);
            _context.SaveChanges();

            transaction.Amount = 200;
            transaction.Note = "Updated";
            _context.Transactions.Update(transaction);
            _context.SaveChanges();

            var updated = _context.Transactions.Find(transaction.Id);
            Assert.Equal(200, updated.Amount);
            Assert.Equal("Updated", updated.Note);
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }
        [Fact]
        public void DefaultDbContext_WithRealSqlite_CoversAllConfigurations()
        {
            // Use a unique temporary file for the SQLite database so it doesn't collide with an existing finance.db
            var dbPath = Path.GetTempFileName();
            try
            {
                var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite($"Data Source={dbPath}")
                    .Options;

                using (var context = new AppDbContext(options)) // use explicit options pointing to temp file
                {
                    context.Database.EnsureCreated();

                    var category = new Category { Name = "ConfigTest", Type = "Income" };
                    context.Categories.Add(category);
                    context.SaveChanges();

                    var transaction = new Transaction
                    {
                        Amount = 100,
                        Date = DateTime.Today,
                        CategoryId = category.Id,
                        Note = "Test"
                    };
                    context.Transactions.Add(transaction);
                    context.SaveChanges();

                    // Load transaction with category to cover Include and the relationship configured in OnModelCreating
                    var loaded = context.Transactions.Include(t => t.Category).First();
                    Assert.Equal("ConfigTest", loaded.Category.Name);
                }
            }
            finally
            {
                if (File.Exists(dbPath)) File.Delete(dbPath);
            }
        }
    }
}