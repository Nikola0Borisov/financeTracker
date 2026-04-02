using System.Collections.Generic;
using FinanceTracker.Data.Entities;

namespace FinanceTracker.Services.Interfaces
{
    /// <summary>
    /// Дефинира операциите за управление на категории.
    /// </summary>
    public interface ICategoryService
    {
        /// <summary>Връща всички категории.</summary>
        IEnumerable<Category> GetAllCategories();

        /// <summary>Връща категория по идентификатор.</summary>
        /// <param name="id">Идентификатор на категорията.</param>
        Category GetCategoryById(int id);

        /// <summary>Добавя нова категория.</summary>
        /// <param name="category">Категорията за добавяне.</param>
        void AddCategory(Category category);

        /// <summary>Актуализира съществуваща категория.</summary>
        /// <param name="category">Категорията с новите данни.</param>
        void UpdateCategory(Category category);

        /// <summary>Изтрива категория по идентификатор.</summary>
        /// <param name="id">Идентификатор на категорията.</param>
        void DeleteCategory(int id);

        /// <summary>Добавя примерни категории, ако таблицата е празна.</summary>
        void SeedDefaultCategories();
    }
}