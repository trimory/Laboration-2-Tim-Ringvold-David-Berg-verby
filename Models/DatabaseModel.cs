using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Data.Sqlite;
using NuGet.Protocol.Plugins;
using System.Text.Json;

namespace Laboration2MVC.Models
{
    public class DatabaseModel
    {
        //sets the database filepath
        public string databaseFilePath = "userTransactions.db";

        
        async public Task DeleteCustomRules()
        {

            if (File.Exists(databaseFilePath))
            {
               
                using var connection = new SqliteConnection($"Data Source={databaseFilePath}");

                await connection.OpenAsync();
                using var deleteCustomRules = connection.CreateCommand();
                deleteCustomRules.CommandText = "DELETE FROM CustomCategory; DELETE FROM TransactionCategory; " +
                                              "UPDATE transactions SET Category = 'Övrigt', UserOverwrittenCategory = 0;";
                await deleteCustomRules.ExecuteNonQueryAsync();
                await connection.CloseAsync();

                

            }
            else Console.WriteLine("Database does not exist");



        }
        public async Task<bool> CheckIfDatabaseEmpty()
        {
            using var connection = new SqliteConnection($"Data Source={databaseFilePath}");

            try
            {
                await connection.OpenAsync();
                using var checkIfEmpty = connection.CreateCommand();
                checkIfEmpty.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'"; //checks if the database is empty
                var result = await checkIfEmpty.ExecuteScalarAsync();

                var count = Convert.ToInt32(result);
                Console.WriteLine();

                return count == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking if database is empty: {ex.Message}");
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        async public Task CreateDatabase()
        {
            using var connection = new SqliteConnection($"Data Source={databaseFilePath}");
            try
            {

                
                    await connection.OpenAsync();
                    using var createTransactionTable = connection.CreateCommand();
                    createTransactionTable.CommandText = @"CREATE TABLE IF NOT EXISTS transactions " +
                        "(TransactionID INTEGER PRIMARY KEY NOT NULL," +
                        " bookingDate TEXT, transactionDate TEXT NOT NULL," +
                        " Reference TEXT, Amount REAL, Balance REAL NOT NULL, " +
                        "Category TEXT, " +
                        "UserOverwrittenCategory BIT)";
                    await createTransactionTable.ExecuteNonQueryAsync();


                    using var CreateCustomCategory = connection.CreateCommand();
                    CreateCustomCategory.CommandText = @"
                    CREATE TABLE IF NOT EXISTS CustomCategory 
                    (
                        OriginalCategory TEXT NOT NULL UNIQUE,
                        NewCategory TEXT NOT NULL
                    )";

                    await CreateCustomCategory.ExecuteNonQueryAsync();

                    using var CreateTransactionCategory = connection.CreateCommand();
                    CreateTransactionCategory.CommandText = @"
                    CREATE TABLE IF NOT EXISTS TransactionCategory 
                    (
                        TransactionID INTEGER PRIMARY KEY NOT NULL,
                        Category TEXT NOT NULL
                    )";
                    await CreateTransactionCategory.ExecuteNonQueryAsync();

                
            }
            catch (Exception)
            {
                Console.WriteLine("error creating database");
                throw;
            }
            finally
            {

                await connection.CloseAsync();
            }
        }
        async public Task CreateCustomCategory(List<string> originalCategories, string newCategory)
        {
            using var connection = new SqliteConnection($"Data Source={databaseFilePath}");

            if (originalCategories == null || !originalCategories.Any())
            {
                throw new ArgumentException("originalCategories cannot be null or empty.");
            }
            if (string.IsNullOrEmpty(newCategory))
            {
                throw new ArgumentException("newCategory cannot be null or empty.");
            }

            try
            {
                await connection.OpenAsync();

                
                foreach (var originalCategory in originalCategories)
                {
                    using var upsertCommand = connection.CreateCommand();
                    upsertCommand.CommandText = @"
                        INSERT OR IGNORE INTO CustomCategory (OriginalCategory, NewCategory)
                        VALUES (@OriginalCategory, @NewCategory);
                        UPDATE CustomCategory
                        SET NewCategory = @NewCategory
                        WHERE OriginalCategory = @OriginalCategory";
                    upsertCommand.Parameters.AddWithValue("@OriginalCategory", originalCategory);
                    upsertCommand.Parameters.AddWithValue("@NewCategory", newCategory);
                    await upsertCommand.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not upsert custom categories: {ex.Message}");
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        async public Task<List<CustomCategoryModel>> GetCustomRules()
        {
            using var connection = new SqliteConnection($"Data Source={databaseFilePath}");

            List<CustomCategoryModel> customCategories = new List<CustomCategoryModel>();
            try
            {
                await connection.OpenAsync();
                using var getCustomCategories = connection.CreateCommand();
                getCustomCategories.CommandText = "SELECT * FROM CustomCategory";
                using var reader = await getCustomCategories.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var originalCategoriesString = reader.GetString(0);
                    var originalCategoriesList = originalCategoriesString.Split(',')
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToList();
                    var newCategory = reader.GetString(1);
                    customCategories.Add(new CustomCategoryModel
                    {
                        OriginalCategories = originalCategoriesList,
                        NewCategory = newCategory
                    });
                }
            }
            catch (Exception)
            {
                Console.WriteLine("could not retrieve custom categories");
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }
            return customCategories;
        }

        async public Task<List<TransactionCategoryModel>> GetTransactionRules()
        {
            using var connection = new SqliteConnection($"Data Source={databaseFilePath}");

            List<TransactionCategoryModel> customCategories = new List<TransactionCategoryModel>();
            try
            {
                await connection.OpenAsync();
                using var getCustomCategories = connection.CreateCommand();
                getCustomCategories.CommandText = "SELECT * FROM TransactionCategory";
                using var reader = await getCustomCategories.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var transactionId = reader.GetInt32(0); 
                    var category = reader.GetString(1);    

                    customCategories.Add(new TransactionCategoryModel
                    {
                        TransactionID = transactionId,
                        Category = category
                    });
                }
            }
            catch (Exception)
            {
                Console.WriteLine("could not retrieve custom categories");
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }
            return customCategories;
        }

        public async Task ApplyCustomRulesToTransactions()
        {
            // Part 1: Update by reference using category‐based rules (only if not manually overridden)
            var customCategoryRules = await GetCustomRules();
            using (var connection = new SqliteConnection($"Data Source={databaseFilePath}"))
            {
                await connection.OpenAsync();
                foreach (var rule in customCategoryRules)
                {
                    foreach (var oldReference in rule.OriginalCategories)
                    {
                        using var command = connection.CreateCommand();
                        command.CommandText = @"
                    UPDATE transactions
                    SET Category = @newCategory
                    WHERE Reference = @oldReference AND UserOverwrittenCategory = 0";
                        command.Parameters.AddWithValue("@newCategory", rule.NewCategory);
                        command.Parameters.AddWithValue("@oldReference", oldReference);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                await connection.CloseAsync();
            }

            var transactionCategoryRules = await GetTransactionRules();
            using (var connection2 = new SqliteConnection($"Data Source={databaseFilePath}"))
            {
                await connection2.OpenAsync();
                foreach (var rule in transactionCategoryRules)
                {
                    using var command = connection2.CreateCommand();
                    command.CommandText = @"
                UPDATE transactions
                SET Category = @newCategory, UserOverwrittenCategory = 1
                WHERE TransactionID = @transactionID";
                    command.Parameters.AddWithValue("@newCategory", rule.Category);
                    command.Parameters.AddWithValue("@transactionID", rule.TransactionID);
                    await command.ExecuteNonQueryAsync();
                }
                await connection2.CloseAsync();
            }
        }

        public async Task InsertTransaction(TransactionModel transaction)
        {
            using var connection = new SqliteConnection($"Data Source={databaseFilePath}");

            try
            {

                await connection.OpenAsync();
                using var insertTransaction = connection.CreateCommand();
                insertTransaction.CommandText = @"INSERT OR IGNORE INTO transactions 
                (TransactionID, bookingDate, transactionDate, Reference, Amount, Balance, Category, UserOverwrittenCategory) 
                VALUES (@TransactionID, @bookingDate, @transactionDate, @Reference,
                @Amount, @Balance, @Category, @UserOverwrittenCategory)";
                insertTransaction.Parameters.AddWithValue("@TransactionID", transaction.TransactionID);
                insertTransaction.Parameters.AddWithValue("@bookingDate", transaction.BookingDate);
                insertTransaction.Parameters.AddWithValue("@transactionDate", transaction.TransactionDate);
                insertTransaction.Parameters.AddWithValue("@Reference", transaction.Reference);
                insertTransaction.Parameters.AddWithValue("@Amount", transaction.Amount);
                insertTransaction.Parameters.AddWithValue("@Balance", transaction.Balance);
                insertTransaction.Parameters.AddWithValue("@Category", transaction.Category);
                //converts bool to int, here represented as bit 
                insertTransaction.Parameters.AddWithValue("@UserOverwrittenCategory", transaction.UserOverwrittenCategory ? 1 : 0);
                await insertTransaction.ExecuteNonQueryAsync();
            }
            catch (Exception)
            {
                Console.WriteLine("error inserting transaction");
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }


        }

       
        public async Task<List<TransactionModel>> GetTransactions()
        {
            List<TransactionModel> transactions = new List<TransactionModel>();
            using var connection = new SqliteConnection($"Data Source={databaseFilePath}");

            try
            {
                await connection.OpenAsync();
                using var getTransactions = connection.CreateCommand();
                getTransactions.CommandText = "SELECT * FROM transactions";
                using var reader = await getTransactions.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    transactions.Add(new TransactionModel
                    {
                        TransactionID = reader.GetInt32(0),
                        BookingDate = reader.GetString(1),
                        TransactionDate = reader.GetString(2),
                        Reference = reader.GetString(3),
                        Amount = reader.GetDouble(4),
                        Balance = reader.GetDouble(5),
                        Category = reader.GetString(6),
                        UserOverwrittenCategory = reader.GetInt32(7) == 1 //converts the bit representation of the
                                                                          //bool in the database to a bool



                    });
                }
            }
            catch (Exception)
            {
                Console.WriteLine("could not retrieve transactions");
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }
            return transactions;
        }
        public async Task<List<ReferenceModel>> GetUniqueReferences() //returns all unique references
        {
            List<ReferenceModel> references = new List<ReferenceModel>();
            using var connection = new SqliteConnection($"Data Source={databaseFilePath}");

            try
            {
                await connection.OpenAsync();
                using var getUniqueReferences = connection.CreateCommand();
                getUniqueReferences.CommandText = "SELECT DISTINCT Reference FROM transactions";

                using var reader = await getUniqueReferences.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    references.Add(new ReferenceModel
                    {
                        Reference = reader.GetString(0) 
                    });
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Error retrieving unique references: ");
            }
            finally
            {
                await connection.CloseAsync();
            }

            return references; 
        }

        async public Task GenerateReport(List<TransactionModel> transactions)
        {
            using var connection = new SqliteConnection($"Data Source={databaseFilePath}");
            try
            {
                await connection.OpenAsync();
                using var getTransactions = connection.CreateCommand();
                getTransactions.CommandText = "SELECT * FROM transactions";
                using var reader = await getTransactions.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var transaction = new TransactionModel
                    {
                        TransactionID = reader.GetInt32(0),
                        BookingDate = reader.GetString(1),
                        TransactionDate = reader.GetString(2),
                        Reference = reader.GetString(3),
                        Amount = reader.GetDouble(4),
                        Balance = reader.GetDouble(5),
                        Category = reader.GetString(6),
                        UserOverwrittenCategory = reader.GetInt32(7) == 1
                    };
                    Console.WriteLine(JsonSerializer.Serialize(transaction));
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Error generating report");
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }
        async public Task CreateCustomCategoryTransactionID(int TransactionID, string newCategory) 
        {
            using var connection = new SqliteConnection($"Data Source={databaseFilePath}");

            if (TransactionID == 0)
            {
                throw new ArgumentException("TransactionID cannot be null or empty.");
            }
            if (string.IsNullOrEmpty(newCategory))
            {
                throw new ArgumentException("newCategory cannot be null or empty.");
            }

            try
            {
                await connection.OpenAsync();


               
                using var UpdateTransaction = connection.CreateCommand();
                UpdateTransaction.CommandText = @"
                    INSERT OR IGNORE INTO TransactionCategory (TransactionID, Category)
                    VALUES (@TransactionID, @Category);
                    UPDATE TransactionCategory
                    SET Category = @Category
                    WHERE TransactionID = @TransactionID";
                UpdateTransaction.Parameters.AddWithValue("@TransactionID", TransactionID);
                UpdateTransaction.Parameters.AddWithValue("@Category", newCategory);
                await UpdateTransaction.ExecuteNonQueryAsync();
                
            }
            catch (Exception)
            {
                Console.WriteLine($"Failed to update custom transaction categories");
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }


    }
}
