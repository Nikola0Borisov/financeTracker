using System.Collections.Generic;
using System.Linq;
using FinanceTracker.Data;
using FinanceTracker.Data.Entities;
using FinanceTracker.Services.Interfaces;

namespace FinanceTracker.Services.Implementations
{
    /// <summary>
    /// Реализация на услугата за управление на категории.
    /// </summary>
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Конструктор, приемащ контекст на базата данни.
        /// </summary>
        /// <param name="context">Контекст на базата данни.</param>
        public CategoryService(AppDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public IEnumerable<Category> GetAllCategories()
        {
            return _context.Categories.ToList();
        }

        /// <inheritdoc />
        public Category GetCategoryById(int id)
        {
            return _context.Categories.Find(id);
        }

        /// <inheritdoc />
        public void AddCategory(Category category)
        {
            _context.Categories.Add(category);
            _context.SaveChanges();
        }

        /// <inheritdoc />
        public void UpdateCategory(Category category)
        {
            _context.Categories.Update(category);
            _context.SaveChanges();
        }

        /// <inheritdoc />
        public void DeleteCategory(int id)
        {
            var category = _context.Categories.Find(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                _context.SaveChanges();
            }
        }

        /// <inheritdoc />
        public void SeedDefaultCategories()
        {
            if (!_context.Categories.Any())
            {
                _context.Categories.AddRange(
                    new Category { Name = "Заплата", Type = "Income" },
                    new Category { Name = "Храна", Type = "Expense" },
                    new Category { Name = "Транспорт", Type = "Expense" },
                    new Category { Name = "Развлечения", Type = "Expense" }
                );
                _context.SaveChanges();
            }
        }
    }
}