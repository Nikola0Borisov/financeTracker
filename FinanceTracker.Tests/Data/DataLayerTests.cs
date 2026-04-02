using System;
using System.Linq;
using System.IO;
using FinanceTracker.Data;
using FinanceTracker.Data.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinanceTracker.Tests.Data
{
    /// <summary>
    /// Тестове за слоя данни – DbContext конфигурации и връзки.
    /// </summary>
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

        // ========== НОВИ ТЕСТОВЕ ЗА ПОВИШАВАНЕ НА ПОКРИТИЕТО ==========



        /// <summary>Тества конфигурацията на връзката между Transaction и Category (OnModelCreating).</summary>
        [Fact]
        public void Relationship_TransactionHasCategory()
        {
            var category = new Category { Name = "RelCat", Type = "Income" };
            _context.Categories.Add(category);
            _context.SaveChanges();

            var transaction = new Transaction
            {
                Amount = 100,
                CategoryId = category.Id
            };
            _context.Transactions.Add(transaction);
            _context.SaveChanges();

            var loaded = _context.Transactions.Include(t => t.Category).First();
            Assert.Equal("RelCat", loaded.Category.Name);
        }

        /// <summary>Тества каскадно изтриване – когато изтрием категория, транзакциите да се изтрият (Cascade).</summary>
        [Fact]
        public void DeleteCategory_CascadesToTransactions()
        {
            var category = new Category { Name = "ToDelete", Type = "Expense" };
            _context.Categories.Add(category);
            _context.SaveChanges();

            var transaction = new Transaction { Amount = -50, CategoryId = category.Id };
            _context.Transactions.Add(transaction);
            _context.SaveChanges();

            _context.Categories.Remove(category);
            _context.SaveChanges();

            Assert.Empty(_context.Categories);
            Assert.Empty(_context.Transactions);
        }

        public void Dispose()
        {
            _context.Dispose();
            _connection.Dispose();
        }
    }
}