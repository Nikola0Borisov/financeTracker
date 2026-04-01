using System.Collections.Generic;
using System.Linq;
using FinanceTracker.Data;
using FinanceTracker.Data.Entities;
using FinanceTracker.Services.Interfaces;

namespace FinanceTracker.Services.Implementations
{
    public class CategoryService : ICategoryService
    {
        private readonly AppDbContext _context;

        /// <summary>
        /// Създава нова инстанция на услугата за категории.
        /// </summary>
        /// <param name="context">Контекст на базата данни</param>
        public CategoryService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Връща всички категории.
        /// </summary>
        /// <returns>Колекция от всички категории</returns>
        public IEnumerable<Category> GetAllCategories()
        {
            return _context.Categories.ToList();
        }

        /// <summary>
        /// Връща категория по идентификатор.
        /// </summary>
        /// <param name="id">Идентификатор на категорията</param>
        /// <returns>Категория или null, ако не е намерена</returns>
        public Category GetCategoryById(int id)
        {
            return _context.Categories.Find(id);
        }

        /// <summary>
        /// Добавя нова категория.
        /// </summary>
        /// <param name="category">Категорията за добавяне</param>
        public void AddCategory(Category category)
        {
            _context.Categories.Add(category);
            _context.SaveChanges();
        }

        /// <summary>
        /// Актуализира съществуваща категория.
        /// </summary>
        /// <param name="category">Категорията с променените данни</param>
        public void UpdateCategory(Category category)
        {
            _context.Categories.Update(category);
            _context.SaveChanges();
        }

        /// <summary>
        /// Изтрива категория по идентификатор.
        /// </summary>
        /// <param name="id">Идентификатор на категорията</param>
        public void DeleteCategory(int id)
        {
            var category = _context.Categories.Find(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                _context.SaveChanges();
            }
        }
    }
}