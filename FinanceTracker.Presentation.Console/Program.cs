using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using FinanceTracker.Data;
using FinanceTracker.Services.Implementations;
using FinanceTracker.Services.Interfaces;

namespace FinanceTracker.Presentation.ConsoleApp
{
    /// <summary>
    /// Главен клас на конзолното приложение – презентационен слой.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Точка на влизане на приложението.
        /// </summary>
        static void Main(string[] args)
        {
            // Настройка за UTF-8 и кирилица
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("bg-BG");
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("bg-BG");

            using var context = AppDbContext.CreateDefault();
            ITransactionService transactionService = new TransactionService(context);
            ICategoryService categoryService = new CategoryService(context);

            // Добавя примерни категории, ако няма
            categoryService.SeedDefaultCategories();

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

        /// <summary>Показва главното меню.</summary>
        static void ShowMainMenu()
        {
            Console.Clear();
            Console.WriteLine("=== ФИНАНСОВ ТРАКЕР ===");
            Console.WriteLine("1. Преглед на транзакции");
            Console.WriteLine("2. Добавяне на транзакция");
            Console.WriteLine("3. Редактиране на транзакция");
            Console.WriteLine("4. Изтриване на транзакции");
            Console.WriteLine("5. Управление на категории");
            Console.WriteLine("6. Статистики");
            Console.WriteLine("7. Експорт към CSV");
            Console.WriteLine("8. Изход");
            Console.Write("Изберете опция: ");
        }

        /// <summary>Показва всички транзакции в табличен вид.</summary>
        /// <param name="service">Услуга за транзакции.</param>
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

        /// <summary>Добавя нова транзакция с валидации.</summary>
        /// <param name="transactionService">Услуга за транзакции.</param>
        /// <param name="categoryService">Услуга за категории.</param>
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

            var selectedCategory = categories.First(c => c.Id == catId);
            bool isExpense = selectedCategory.Type == "Expense";

            decimal amount;
            while (true)
            {
                Console.Write("Сума: ");
                if (!decimal.TryParse(Console.ReadLine(), out amount))
                {
                    Console.WriteLine("Невалидна сума. Моля, въведете число.");
                    continue;
                }

                if (isExpense && amount > 0)
                {
                    Console.WriteLine("Разходът трябва да е отрицателно число (например -25.50). Опитайте отново.");
                    continue;
                }
                if (!isExpense && amount < 0)
                {
                    Console.WriteLine("Приходът трябва да е положително число (например 1500). Опитайте отново.");
                    continue;
                }
                break;
            }

            DateTime date;
            while (true)
            {
                Console.Write("Дата (yyyy-mm-dd): ");
                string dateInput = Console.ReadLine();
                if (!DateTime.TryParseExact(dateInput, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out date))
                {
                    Console.WriteLine("Невалиден формат. Използвайте ГГГГ-ММ-ДД (пример: 2025-04-02).");
                    continue;
                }
                if (date.Year < 2000 || date.Year > 2100)
                {
                    Console.WriteLine("Годината трябва да е между 2000 и 2100.");
                    continue;
                }
                break;
            }

            Console.Write("Бележка (незадължителна, макс. 50 символа): ");
            string note = Console.ReadLine() ?? "";
            if (note.Length > 50)
            {
                note = note.Substring(0, 50);
                Console.WriteLine($"Бележката беше съкратена до 50 символа: {note}");
            }

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

        /// <summary>Редактира съществуваща транзакция – показва списък, валидира сума, дата, бележка.</summary>
        /// <param name="transactionService">Услуга за транзакции.</param>
        /// <param name="categoryService">Услуга за категории.</param>
        static void EditTransaction(ITransactionService transactionService, ICategoryService categoryService)
        {
            Console.Clear();

            var allTransactions = transactionService.GetAllTransactions().ToList();
            if (!allTransactions.Any())
            {
                Console.WriteLine("Няма записани транзакции за редактиране.");
                Wait();
                return;
            }

            Console.WriteLine("=== Списък на транзакциите ===");
            Console.WriteLine("ID | Дата       | Сума   | Категория   | Бележка");
            foreach (var t in allTransactions)
                Console.WriteLine($"{t.Id,-3} | {t.Date:yyyy-MM-dd} | {t.Amount,7:F2} | {t.Category?.Name,-12} | {t.Note}");

            Console.Write("\nВъведете ID на транзакцията за редактиране: ");
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

            Console.WriteLine($"\nРедактиране на транзакция {id}");

            // Сума с валидация според типа категория
            var category = categoryService.GetCategoryById(transaction.CategoryId);
            bool isExpense = category?.Type == "Expense";
            Console.Write($"Сума ({transaction.Amount}): ");
            string amountInput = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(amountInput))
            {
                if (decimal.TryParse(amountInput, out decimal newAmount))
                {
                    if (isExpense && newAmount > 0)
                        Console.WriteLine("Разходът трябва да е отрицателно число. Сумата не е променена.");
                    else if (!isExpense && newAmount < 0)
                        Console.WriteLine("Приходът трябва да е положително число. Сумата не е променена.");
                    else
                        transaction.Amount = newAmount;
                }
                else
                    Console.WriteLine("Невалидна сума. Сумата не е променена.");
            }

            // Дата с валидация
            Console.Write($"Дата ({transaction.Date:yyyy-MM-dd}): ");
            string dateInput = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(dateInput))
            {
                if (DateTime.TryParseExact(dateInput, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out DateTime newDate))
                {
                    if (newDate.Year >= 2000 && newDate.Year <= 2100)
                        transaction.Date = newDate;
                    else
                        Console.WriteLine("Годината трябва да е между 2000 и 2100. Датата не е променена.");
                }
                else
                    Console.WriteLine("Невалиден формат (използвайте yyyy-MM-dd). Датата не е променена.");
            }

            // Бележка с ограничение
            Console.Write($"Бележка ({transaction.Note}): ");
            string note = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(note))
            {
                if (note.Length > 50) note = note.Substring(0, 50);
                transaction.Note = note;
            }

            // Категория – показваме списък
            var categories = categoryService.GetAllCategories().ToList();
            Console.WriteLine("\nСписък с категории:");
            foreach (var cat in categories)
                Console.WriteLine($"{cat.Id}. {cat.Name} ({cat.Type})");
            Console.Write($"Нова категория (текуща: {transaction.Category?.Name}) -> ID: ");
            if (int.TryParse(Console.ReadLine(), out int catId) && categories.Any(c => c.Id == catId))
                transaction.CategoryId = catId;

            transactionService.UpdateTransaction(transaction);
            Console.WriteLine("Транзакцията е обновена.");
            Wait();
        }

        /// <summary>Изтрива една или няколко транзакции след показване на списък и потвърждение.</summary>
        /// <param name="service">Услуга за транзакции.</param>
        static void DeleteTransaction(ITransactionService service)
        {
            Console.Clear();

            var allTransactions = service.GetAllTransactions().ToList();
            if (!allTransactions.Any())
            {
                Console.WriteLine("Няма записани транзакции за изтриване.");
                Wait();
                return;
            }

            Console.WriteLine("=== Списък на транзакциите ===");
            Console.WriteLine("ID | Дата       | Сума   | Категория   | Бележка");
            foreach (var t in allTransactions)
                Console.WriteLine($"{t.Id,-3} | {t.Date:yyyy-MM-dd} | {t.Amount,7:F2} | {t.Category?.Name,-12} | {t.Note}");

            Console.WriteLine("\nВъведете ID/ID-та за изтриване (разделени със запетая, например: 3,5,7)");
            Console.Write("> ");
            string input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Невалиден вход.");
                Wait();
                return;
            }

            var idsRaw = input.Split(',')
                              .Select(part => part.Trim())
                              .Where(part => !string.IsNullOrEmpty(part));

            List<int> idsToDelete = new List<int>();
            foreach (var part in idsRaw)
            {
                if (int.TryParse(part, out int id))
                    idsToDelete.Add(id);
                else
                    Console.WriteLine($"Предупреждение: '{part}' не е валидно число и ще бъде игнорирано.");
            }

            if (idsToDelete.Count == 0)
            {
                Console.WriteLine("Няма валидни ID за изтриване.");
                Wait();
                return;
            }

            var existingIds = allTransactions.Select(t => t.Id).ToHashSet();
            var validIds = idsToDelete.Where(id => existingIds.Contains(id)).ToList();
            var invalidIds = idsToDelete.Where(id => !existingIds.Contains(id)).ToList();

            if (invalidIds.Any())
                Console.WriteLine($"Следните ID не съществуват и няма да бъдат изтрити: {string.Join(", ", invalidIds)}");

            if (validIds.Count == 0)
            {
                Console.WriteLine("Няма съществуващи транзакции за изтриване.");
                Wait();
                return;
            }

            Console.Write($"Сигурни ли сте, че искате да изтриете {validIds.Count} транзакции? (y/n): ");
            if (Console.ReadLine()?.ToLower() != "y")
            {
                Console.WriteLine("Изтриването е отменено.");
                Wait();
                return;
            }

            int deletedCount = 0;
            foreach (var id in validIds)
            {
                service.DeleteTransaction(id);
                deletedCount++;
            }

            Console.WriteLine($"Успешно изтрити {deletedCount} транзакции.");
            Wait();
        }

        /// <summary>Меню за управление на категории – показва списък, изтрива няколко наведнъж.</summary>
        /// <param name="service">Услуга за категории.</param>
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

        /// <summary>Добавя нова категория с избор на тип (0/1).</summary>
        /// <param name="service">Услуга за категории.</param>
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

            Console.WriteLine("Изберете тип:");
            Console.WriteLine("0 - Income (приход)");
            Console.WriteLine("1 - Expense (разход)");
            Console.Write("Вашият избор (0 или 1): ");
            string typeChoice = Console.ReadLine();
            string type = typeChoice switch
            {
                "0" => "Income",
                "1" => "Expense",
                _ => null
            };
            if (type == null)
            {
                Console.WriteLine("Невалиден избор. Трябва да въведете 0 или 1.");
                Wait();
                return;
            }

            service.AddCategory(new Data.Entities.Category { Name = name, Type = type });
            Console.WriteLine("Категорията е добавена.");
            Wait();
        }

        /// <summary>Редактира съществуваща категория – показва списък.</summary>
        /// <param name="service">Услуга за категории.</param>
        static void EditCategory(ICategoryService service)
        {
            Console.Clear();

            var categories = service.GetAllCategories().ToList();
            if (!categories.Any())
            {
                Console.WriteLine("Няма дефинирани категории.");
                Wait();
                return;
            }

            Console.WriteLine("=== Списък на категориите ===");
            Console.WriteLine("ID | Име           | Тип");
            foreach (var cat in categories)
                Console.WriteLine($"{cat.Id,-3} | {cat.Name,-12} | {cat.Type}");

            Console.Write("\nВъведете ID на категорията за редактиране: ");
            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine("Невалидно ID.");
                Wait();
                return;
            }

            // Променено име на променливата, за да няма конфликт с цикъла foreach
            var categoryToEdit = service.GetCategoryById(id);
            if (categoryToEdit == null)
            {
                Console.WriteLine("Категория не е намерена.");
                Wait();
                return;
            }

            Console.Write($"Ново име ({categoryToEdit.Name}): ");
            string newName = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newName))
                categoryToEdit.Name = newName;

            Console.WriteLine("Изберете нов тип:");
            Console.WriteLine("0 - Income (приход)");
            Console.WriteLine("1 - Expense (разход)");
            Console.Write($"Вашият избор (текущ: {categoryToEdit.Type}): ");
            string typeChoice = Console.ReadLine();
            if (typeChoice == "0" || typeChoice == "1")
                categoryToEdit.Type = (typeChoice == "0") ? "Income" : "Expense";

            service.UpdateCategory(categoryToEdit);
            Console.WriteLine("Категорията е обновена.");
            Wait();
        }

        /// <summary>Изтрива една или няколко категории – показва списък, проверява за транзакции.</summary>
        /// <param name="service">Услуга за категории.</param>
        static void DeleteCategory(ICategoryService service)
        {
            Console.Clear();

            var categories = service.GetAllCategories().ToList();
            if (!categories.Any())
            {
                Console.WriteLine("Няма дефинирани категории.");
                Wait();
                return;
            }

            Console.WriteLine("=== Списък на категориите ===");
            Console.WriteLine("ID | Име           | Тип");
            foreach (var cat in categories)
                Console.WriteLine($"{cat.Id,-3} | {cat.Name,-12} | {cat.Type}");

            Console.WriteLine("\nВъведете ID/ID-та за изтриване (разделени със запетая, например: 3,5,7)");
            Console.Write("> ");
            string input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Невалиден вход.");
                Wait();
                return;
            }

            var idsRaw = input.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p));
            List<int> idsToDelete = new List<int>();
            foreach (var part in idsRaw)
            {
                if (int.TryParse(part, out int id))
                    idsToDelete.Add(id);
                else
                    Console.WriteLine($"Предупреждение: '{part}' не е валидно число и ще бъде игнорирано.");
            }

            if (idsToDelete.Count == 0)
            {
                Console.WriteLine("Няма валидни ID за изтриване.");
                Wait();
                return;
            }

            var existingIds = categories.Select(c => c.Id).ToHashSet();
            var validIds = idsToDelete.Where(id => existingIds.Contains(id)).ToList();
            var invalidIds = idsToDelete.Where(id => !existingIds.Contains(id)).ToList();

            if (invalidIds.Any())
                Console.WriteLine($"Следните ID не съществуват: {string.Join(", ", invalidIds)}");

            if (validIds.Count == 0)
            {
                Console.WriteLine("Няма съществуващи категории за изтриване.");
                Wait();
                return;
            }

            Console.Write($"Сигурни ли сте, че искате да изтриете {validIds.Count} категории? (y/n): ");
            if (Console.ReadLine()?.ToLower() != "y")
            {
                Console.WriteLine("Изтриването е отменено.");
                Wait();
                return;
            }

            foreach (var id in validIds)
                service.DeleteCategory(id);

            Console.WriteLine($"Успешно изтрити {validIds.Count} категории.");
            Wait();
        }

        /// <summary>Показва статистики за приходи/разходи за даден период.</summary>
        /// <param name="service">Услуга за транзакции.</param>
        static void ShowStatistics(ITransactionService service)
        {
            Console.Clear();
            DateTime start, end;
            while (true)
            {
                Console.Write("Начална дата (yyyy-mm-dd): ");
                string startInput = Console.ReadLine();
                if (!DateTime.TryParseExact(startInput, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out start))
                {
                    Console.WriteLine("Невалиден формат. Използвайте ГГГГ-ММ-ДД.");
                    continue;
                }
                Console.Write("Крайна дата (yyyy-mm-dd): ");
                string endInput = Console.ReadLine();
                if (!DateTime.TryParseExact(endInput, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out end))
                {
                    Console.WriteLine("Невалиден формат. Използвайте ГГГГ-ММ-ДД.");
                    continue;
                }
                if (start > end)
                {
                    Console.WriteLine("Началната дата не може да е след крайната.");
                    continue;
                }
                break;
            }

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

        /// <summary>Експортира всички транзакции в CSV файл – създава папка Exports, ако няма права.</summary>
        /// <param name="service">Услуга за транзакции.</param>
        static void ExportTransactions(ITransactionService service)
        {
            Console.Clear();

            // Създава папка "Exports" в текущата директория
            string exportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Exports");
            if (!Directory.Exists(exportDir))
                Directory.CreateDirectory(exportDir);

            string defaultPath = Path.Combine(exportDir, $"transactions_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            Console.WriteLine($"Ще бъде използван път по подразбиране: {defaultPath}");
            Console.Write("Въведете пълен път до CSV файл (Enter за използване на подразбиращия се): ");
            string path = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(path))
                path = defaultPath;

            var transactions = service.GetAllTransactions();
            try
            {
                service.ExportToCsv(path, transactions);
                Console.WriteLine($"Експортирани {transactions.Count()} транзакции във файл {path}");
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine("Нямате права да пишете в тази папка. Използвайте папка като Desktop или Documents.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Грешка при експорт: {ex.Message}");
            }
            Wait();
        }

        /// <summary>Изчаква натискане на клавиш, за да продължи.</summary>
        static void Wait()
        {
            Console.WriteLine("\nНатиснете произволен клавиш...");
            Console.ReadKey();
        }
    }
}