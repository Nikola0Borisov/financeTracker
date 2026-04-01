using System;
using System.Linq;
using FinanceTracker.Data;
using FinanceTracker.Services.Implementations;
using FinanceTracker.Services.Interfaces;

namespace FinanceTracker.Presentation.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
            // Използваме фабричния метод за SQLite база
            using var context = AppDbContext.CreateDefault();
            ITransactionService transactionService = new TransactionService(context);
            ICategoryService categoryService = new CategoryService(context);

            // Добавяме примерни категории, ако няма
            if (!context.Categories.Any())
            {
                context.Categories.AddRange(
                    new Data.Entities.Category { Name = "Заплата", Type = "Income" },
                    new Data.Entities.Category { Name = "Храна", Type = "Expense" },
                    new Data.Entities.Category { Name = "Транспорт", Type = "Expense" },
                    new Data.Entities.Category { Name = "Развлечения", Type = "Expense" }
                );
                context.SaveChanges();
            }

            bool exit = false;
            while (!exit)
            {
                ShowMainMenu();
                var choice = Console.ReadLine();
                switch (choice)
                {
                    case "1": ShowTransactions(transactionService); break;
                    case "2": AddTransaction(transactionService, categoryService); break;
                    case "3": EditTransaction(transactionService, categoryService); break;
                    case "4": DeleteTransaction(transactionService); break;
                    case "5": ManageCategories(categoryService); break;
                    case "6": ShowStatistics(transactionService); break;
                    case "7": ExportTransactions(transactionService); break;
                    case "8": exit = true; break;
                    default: Console.WriteLine("Невалиден избор."); Wait(); break;
                }
            }
        }

        static void ShowMainMenu()
        {
            Console.Clear();
            Console.WriteLine("=== ФИНАНСОВ ТРАКЕР ===");
            Console.WriteLine("1. Преглед на транзакции");
            Console.WriteLine("2. Добавяне на транзакция");
            Console.WriteLine("3. Редактиране на транзакция");
            Console.WriteLine("4. Изтриване на транзакция");
            Console.WriteLine("5. Управление на категории");
            Console.WriteLine("6. Статистики");
            Console.WriteLine("7. Експорт към CSV");
            Console.WriteLine("8. Изход");
            Console.Write("Изберете опция: ");
        }

        static void ShowTransactions(ITransactionService service)
        {
            Console.Clear();
            var transactions = service.GetAllTransactions().ToList();
            if (!transactions.Any())
                Console.WriteLine("Няма записани транзакции.");
            else
            {
                Console.WriteLine("ID | Дата       | Сума   | Категория   | Бележка");
                foreach (var t in transactions)
                    Console.WriteLine($"{t.Id,-3} | {t.Date:yyyy-MM-dd} | {t.Amount,7:F2} | {t.Category?.Name,-12} | {t.Note}");
            }
            Wait();
        }

        static void AddTransaction(ITransactionService transactionService, ICategoryService categoryService)
        {
            Console.Clear();
            Console.WriteLine("=== Добавяне на транзакция ===");

            var categories = categoryService.GetAllCategories().ToList();
            if (!categories.Any())
            {
                Console.WriteLine("Няма дефинирани категории. Първо добавете категории от меню 5.");
                Wait();
                return;
            }

            Console.WriteLine("Категории:");
            foreach (var cat in categories)
                Console.WriteLine($"{cat.Id}. {cat.Name} ({cat.Type})");
            Console.Write("Изберете ID на категория: ");
            if (!int.TryParse(Console.ReadLine(), out int catId) || !categories.Any(c => c.Id == catId))
            {
                Console.WriteLine("Невалиден избор.");
                Wait();
                return;
            }

            Console.Write("Сума (положителна за приход, отрицателна за разход): ");
            if (!decimal.TryParse(Console.ReadLine(), out decimal amount))
            {
                Console.WriteLine("Невалидна сума.");
                Wait();
                return;
            }

            Console.Write("Дата (yyyy-mm-dd): ");
            if (!DateTime.TryParse(Console.ReadLine(), out DateTime date))
                date = DateTime.Today;

            Console.Write("Бележка (незадължителна): ");
            string note = Console.ReadLine();

            var transaction = new Data.Entities.Transaction
            {
                Amount = amount,
                Date = date,
                Note = note,
                CategoryId = catId
            };

            transactionService.AddTransaction(transaction);
            Console.WriteLine("Транзакцията е добавена.");
            Wait();
        }

        static void EditTransaction(ITransactionService transactionService, ICategoryService categoryService)
        {
            Console.Clear();
            Console.Write("Въведете ID на транзакцията за редактиране: ");
            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine("Невалидно ID.");
                Wait();
                return;
            }

            var transaction = transactionService.GetTransactionById(id);
            if (transaction == null)
            {
                Console.WriteLine("Транзакция не е намерена.");
                Wait();
                return;
            }

            Console.WriteLine($"Редактиране на транзакция {id}");
            Console.Write($"Сума ({transaction.Amount}): ");
            if (decimal.TryParse(Console.ReadLine(), out decimal newAmount))
                transaction.Amount = newAmount;

            Console.Write($"Дата ({transaction.Date:yyyy-MM-dd}): ");
            if (DateTime.TryParse(Console.ReadLine(), out DateTime newDate))
                transaction.Date = newDate;

            Console.Write($"Бележка ({transaction.Note}): ");
            string note = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(note))
                transaction.Note = note;

            var categories = categoryService.GetAllCategories().ToList();
            Console.WriteLine("Категории:");
            foreach (var cat in categories)
                Console.WriteLine($"{cat.Id}. {cat.Name} ({cat.Type})");
            Console.Write($"Нова категория (текуща: {transaction.Category?.Name}) -> ID: ");
            if (int.TryParse(Console.ReadLine(), out int catId) && categories.Any(c => c.Id == catId))
                transaction.CategoryId = catId;

            transactionService.UpdateTransaction(transaction);
            Console.WriteLine("Транзакцията е обновена.");
            Wait();
        }

        static void DeleteTransaction(ITransactionService service)
        {
            Console.Clear();
            Console.Write("Въведете ID на транзакцията за изтриване: ");
            if (int.TryParse(Console.ReadLine(), out int id))
            {
                service.DeleteTransaction(id);
                Console.WriteLine("Транзакцията е изтрита.");
            }
            else
                Console.WriteLine("Невалидно ID.");
            Wait();
        }

        static void ManageCategories(ICategoryService service)
        {
            bool back = false;
            while (!back)
            {
                Console.Clear();
                Console.WriteLine("=== Управление на категории ===");
                var categories = service.GetAllCategories().ToList();
                Console.WriteLine("Списък с категории:");
                foreach (var cat in categories)
                    Console.WriteLine($"{cat.Id}. {cat.Name} ({cat.Type})");
                Console.WriteLine("\n1. Добави категория");
                Console.WriteLine("2. Редактирай категория");
                Console.WriteLine("3. Изтрий категория");
                Console.WriteLine("4. Назад");
                Console.Write("Избор: ");
                var choice = Console.ReadLine();
                switch (choice)
                {
                    case "1":
                        AddCategory(service);
                        break;
                    case "2":
                        EditCategory(service);
                        break;
                    case "3":
                        DeleteCategory(service);
                        break;
                    case "4":
                        back = true;
                        break;
                    default:
                        Console.WriteLine("Невалиден избор.");
                        Wait();
                        break;
                }
            }
        }

        static void AddCategory(ICategoryService service)
        {
            Console.Clear();
            Console.Write("Име на категория: ");
            string name = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(name))
            {
                Console.WriteLine("Името не може да е празно.");
                Wait();
                return;
            }
            Console.Write("Тип (Income/Expense): ");
            string type = Console.ReadLine();
            if (type != "Income" && type != "Expense")
            {
                Console.WriteLine("Типът трябва да е 'Income' или 'Expense'.");
                Wait();
                return;
            }
            service.AddCategory(new Data.Entities.Category { Name = name, Type = type });
            Console.WriteLine("Категорията е добавена.");
            Wait();
        }

        static void EditCategory(ICategoryService service)
        {
            Console.Clear();
            Console.Write("ID на категорията за редактиране: ");
            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine("Невалидно ID.");
                Wait();
                return;
            }
            var cat = service.GetCategoryById(id);
            if (cat == null)
            {
                Console.WriteLine("Категория не е намерена.");
                Wait();
                return;
            }
            Console.Write($"Ново име ({cat.Name}): ");
            string newName = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newName))
                cat.Name = newName;
            Console.Write($"Нов тип ({cat.Type}): ");
            string newType = Console.ReadLine();
            if (newType == "Income" || newType == "Expense")
                cat.Type = newType;
            service.UpdateCategory(cat);
            Console.WriteLine("Категорията е обновена.");
            Wait();
        }

        static void DeleteCategory(ICategoryService service)
        {
            Console.Clear();
            Console.Write("ID на категорията за изтриване: ");
            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine("Невалидно ID.");
                Wait();
                return;
            }
            service.DeleteCategory(id);
            Console.WriteLine("Категорията е изтрита.");
            Wait();
        }

        static void ShowStatistics(ITransactionService service)
        {
            Console.Clear();
            Console.Write("Начална дата (yyyy-mm-dd): ");
            DateTime start;
            while (!DateTime.TryParse(Console.ReadLine(), out start))
                Console.Write("Невалидна дата. Въведете отново: ");
            Console.Write("Крайна дата (yyyy-mm-dd): ");
            DateTime end;
            while (!DateTime.TryParse(Console.ReadLine(), out end))
                Console.Write("Невалидна дата. Въведете отново: ");

            var expenses = service.GetExpensesByCategory(start, end);
            var incomes = service.GetIncomesByCategory(start, end);
            decimal balance = service.GetBalance(end);

            Console.WriteLine($"\n=== Статистики за период {start:yyyy-MM-dd} - {end:yyyy-MM-dd} ===");
            Console.WriteLine($"Баланс: {balance:F2} лв.");
            Console.WriteLine("\nПриходи по категории:");
            foreach (var inc in incomes)
                Console.WriteLine($"  {inc.Key}: {inc.Value:F2} лв.");
            Console.WriteLine("\nРазходи по категории:");
            foreach (var exp in expenses)
                Console.WriteLine($"  {exp.Key}: {exp.Value:F2} лв.");
            Wait();
        }

        static void ExportTransactions(ITransactionService service)
        {
            Console.Clear();
            Console.Write("Въведете пълен път до CSV файл (напр. C:\\temp\\export.csv): ");
            string path = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(path))
            {
                Console.WriteLine("Невалиден път.");
                Wait();
                return;
            }

            var transactions = service.GetAllTransactions();
            try
            {
                service.ExportToCsv(path, transactions);
                Console.WriteLine($"Експортирани {transactions.Count()} транзакции във файл {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Грешка при експорт: {ex.Message}");
            }
            Wait();
        }

        static void Wait()
        {
            Console.WriteLine("\nНатиснете произволен клавиш...");
            Console.ReadKey();
        }
    }
}