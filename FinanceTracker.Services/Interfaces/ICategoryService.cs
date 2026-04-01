using System.Collections.Generic;
using FinanceTracker.Data.Entities;

namespace FinanceTracker.Services.Interfaces
{
    public interface ICategoryService
    {
        /// <summary>
        /// Връща всички категории.
        /// </summary>
        /// <returns>Колекция от всички категории</returns>
        IEnumerable<Category> GetAllCategories();

        /// <summary>
        /// Връща категория по идентификатор.
        /// </summary>
        /// <param name="id">Идентификатор на категорията</param>
        /// <returns>Категория или null, ако не е намерена</returns>
        Category GetCategoryById(int id);

        /// <summary>
        /// Добавя нова категория.
        /// </summary>
        /// <param name="category">Категорията за добавяне</param>
        void AddCategory(Category category);

        /// <summary>
        /// Актуализира съществуваща категория.
        /// </summary>
        /// <param name="category">Категорията с променените данни</param>
        void UpdateCategory(Category category);

        /// <summary>
        /// Изтрива категория по идентификатор.
        /// </summary>
        /// <param name="id">Идентификатор на категорията</param>
        void DeleteCategory(int id);
    }
}