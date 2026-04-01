using Microsoft.EntityFrameworkCore;
using FinanceTracker.Data.Entities;

namespace FinanceTracker.Data
{
    public class AppDbContext : DbContext
    {
        // Конструктор, приемащ опции (използва се от тестовете)
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CategoryId);
        }

        // Фабричен метод за приложението (SQLite)
        public static AppDbContext CreateDefault()
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlite("Data Source=finance.db");
            return new AppDbContext(optionsBuilder.Options);
        }
    }
}