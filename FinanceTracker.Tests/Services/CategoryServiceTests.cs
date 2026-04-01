using System.Linq;
using FinanceTracker.Data;
using FinanceTracker.Data.Entities;
using FinanceTracker.Services.Implementations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FinanceTracker.Tests.Services
{
    public class CategoryServiceTests : IDisposable
    {
        private readonly AppDbContext _context;
        private readonly CategoryService _service;

        public CategoryServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;
            _context = new AppDbContext(options);
            _service = new CategoryService(_context);
        }

        [Fact]
        public void AddCategory_ShouldIncreaseCount()
        {
            var category = new Category { Name = "Тест", Type = "Expense" };
            _service.AddCategory(category);
            Assert.Equal(1, _context.Categories.Count());
            Assert.Equal("Тест", _context.Categories.First().Name);
        }

        [Fact]
        public void GetAllCategories_ShouldReturnAll()
        {
            _context.Categories.AddRange(
                new Category { Name = "Кат1", Type = "Income" },
                new Category { Name = "Кат2", Type = "Expense" }
            );
            _context.SaveChanges();

            var result = _service.GetAllCategories();
            Assert.Equal(2, result.Count());
            Assert.Contains(result, c => c.Name == "Кат1");
            Assert.Contains(result, c => c.Name == "Кат2");
        }

        [Fact]
        public void GetCategoryById_ShouldReturnCorrectCategory()
        {
            var category = new Category { Name = "Целева", Type = "Income" };
            _context.Categories.Add(category);
            _context.SaveChanges();

            var result = _service.GetCategoryById(category.Id);
            Assert.NotNull(result);
            Assert.Equal("Целева", result.Name);
        }

        [Fact]
        public void GetCategoryById_WhenNotExists_ShouldReturnNull()
        {
            var result = _service.GetCategoryById(999);
            Assert.Null(result);
        }

        [Fact]
        public void UpdateCategory_ShouldModifyCategory()
        {
            var category = new Category { Name = "Стар", Type = "Expense" };
            _context.Categories.Add(category);
            _context.SaveChanges();

            category.Name = "Нов";
            category.Type = "Income";
            _service.UpdateCategory(category);

            var updated = _context.Categories.Find(category.Id);
            Assert.Equal("Нов", updated.Name);
            Assert.Equal("Income", updated.Type);
        }

        [Fact]
        public void DeleteCategory_ShouldRemoveCategory()
        {
            var category = new Category { Name = "За изтриване", Type = "Expense" };
            _context.Categories.Add(category);
            _context.SaveChanges();
            int id = category.Id;

            _service.DeleteCategory(id);

            Assert.Null(_context.Categories.Find(id));
        }

        [Fact]
        public void DeleteCategory_WhenNotExists_ShouldNotThrow()
        {
            // Просто проверяваме, че няма изключение
            _service.DeleteCategory(999);
        }

        public void Dispose()
        {
            _context.Dispose();
        }

    }
}