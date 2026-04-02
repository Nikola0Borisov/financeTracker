using Microsoft.EntityFrameworkCore;
using FinanceTracker.Data.Entities;

namespace FinanceTracker.Data
{
    /// <summary>
    /// Контекст на базата данни за Entity Framework Core.
    /// </summary>
    public class AppDbContext : DbContext
    {
        /// <summary>
        /// Конструктор с опции (използва се за Dependency Injection и тестове).
        /// </summary>
        /// <param name="options">Настройки на контекста.</param>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        /// <summary>
        /// Конструктор по подразбиране – използва SQLite файл "finance.db".
        /// </summary>
        public AppDbContext()
            : this(new DbContextOptionsBuilder<AppDbContext>()
                  .UseSqlite("Data Source=finance.db")
                  .Options)
        { }

        /// <summary>
        /// Таблица с категории.
        /// </summary>
        public DbSet<Category> Categories { get; set; }

        /// <summary>
        /// Таблица с транзакции.
        /// </summary>
        public DbSet<Transaction> Transactions { get; set; }

        /// <summary>
        /// Конфигуриране на връзките между таблиците.
        /// </summary>
        /// <param name="modelBuilder">Обект за изграждане на модела.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CategoryId);
        }

        /// <summary>
        /// Фабричен метод за създаване на контекст по подразбиране (SQLite).
        /// </summary>
        /// <returns>Нова инстанция на AppDbContext.</returns>
        public static AppDbContext CreateDefault()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite("Data Source=finance.db");
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}