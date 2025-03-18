using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Data.Sqlite;
using NuGet.Protocol.Plugins;
using System.Text.Json;

namespace Laboration2MVC.Models
{
    public class DatabaseModel
    {
        private readonly SqliteConnection sqlite;
        public string databaseFilePath = "userTransactions.db";

        public DatabaseModel()
        {
            sqlite = new SqliteConnection($"Data Source={databaseFilePath}");
        }

       
        async public Task CreateDatabase()
        {
            try
            {
                if (!File.Exists(databaseFilePath))
                {
                    
                    await sqlite.OpenAsync();
                    using var createTable = sqlite.CreateCommand();
                    createTable.CommandText = @"CREATE TABLE IF NOT EXISTS transactions " +
                        "(TransactionID INTEGER PRIMARY KEY NOT NULL," +
                        " bookingDate TEXT, transactionDate TEXT NOT NULL," +
                        " Reference TEXT, Amount REAL, Balance REAL NOT NULL, " +
                        "Category TEXT, " +
                        "UserOverwrittenCategory BIT)";
                    await createTable.ExecuteNonQueryAsync();


                    using var InitiateCustomCategory = sqlite.CreateCommand();
                    InitiateCustomCategory.CommandText = @"
                    CREATE TABLE IF NOT EXISTS CustomCategory 
                    (
                        OriginalCategory TEXT NOT NULL UNIQUE,
                        NewCategory TEXT NOT NULL
                    )";
                    await InitiateCustomCategory.ExecuteNonQueryAsync();


                }
                else Console.WriteLine("Database already exists");
            }
            catch (Exception)
            {
                Console.WriteLine("error creating database");
                throw;
            }
            finally
            {
                await sqlite.CloseAsync();
            }
        }
        async public Task CreateCustomCategory(List<string> originalCategories, string newCategory)
        {
            if (originalCategories == null || !originalCategories.Any())
            {
                throw new ArgumentException("originalCategories cannot be null or empty.");
            }
            if (string.IsNullOrEmpty(newCategory))
            {
                throw new ArgumentException("newCategory cannot be null or empty.");
            }

            using var connection = new SqliteConnection($"Data Source=userTransactions.db");
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
                await sqlite.CloseAsync();
            }
        }

        async public Task<List<CustomCategoryModel>> GetCustomRules()
        {
            List<CustomCategoryModel> customCategories = new List<CustomCategoryModel>();
            try
            {
                await sqlite.OpenAsync();
                using var getCustomCategories = sqlite.CreateCommand();
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
                await sqlite.CloseAsync();
            }
            return customCategories;
        }


        public async Task ApplyCustomRulesToTransactions()
        {
            // 1) Retrieve all custom category rules from the DB
            var rules = await GetCustomRules();

            try
            {
                // 2) Open connection once before applying all rules
                await sqlite.OpenAsync();

                // 3) For each rule
                foreach (var rule in rules)
                {
                    // For each old category in the rule's OriginalCategories
                    foreach (var oldReference in rule.OriginalCategories)
                    {
                        // 4) Update the transactions table
                        using var command = sqlite.CreateCommand();
                        command.CommandText = @"
                    UPDATE transactions
                    SET Category = @newCategory
                    WHERE Reference = @oldReference AND UserOverWrittenCategory = 0";

                        command.Parameters.AddWithValue("@newCategory", rule.NewCategory);
                        command.Parameters.AddWithValue("@oldReference", oldReference);

                        await command.ExecuteNonQueryAsync();
                    }
                }
                Console.WriteLine("✅ Successfully applied custom rules to transactions.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Could not apply custom rules: {ex.Message}");
                throw;
            }
            finally
            {
                // 5) Close the connection
                await sqlite.CloseAsync();
            }
        }

        public async Task InsertTransaction(TransactionModel transaction)
        {
            try
            {

                await sqlite.OpenAsync();
                using var insertTransaction = sqlite.CreateCommand();
                insertTransaction.CommandText = @"INSERT INTO transactions 
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
                await sqlite.CloseAsync();
            }


        }

       
        public async Task<List<TransactionModel>> GetTransactions()
        {
            List<TransactionModel> transactions = new List<TransactionModel>();
            try
            {
                await sqlite.OpenAsync();
                using var getTransactions = sqlite.CreateCommand();
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
                await sqlite.CloseAsync();
            }
            return transactions;
        }
        public async Task<List<ReferenceModel>> GetUniqueReferences() //returns all unique references
        {
            List<ReferenceModel> references = new List<ReferenceModel>();

            try
            {
                await sqlite.OpenAsync();
                using var getUniqueReferences = sqlite.CreateCommand();
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
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error retrieving unique references: {ex.Message}");
            }
            finally
            {
                await sqlite.CloseAsync();
            }

            return references; 
        }

        public async Task UpdateDatabaseCategories(List<string> oldReferences, string newReference)
        {
            try
            {
                await sqlite.OpenAsync();

                foreach (var oldReference in oldReferences)
                {
                    using var replaceReferences = sqlite.CreateCommand();
                    replaceReferences.CommandText = "UPDATE transactions " +
                        "SET Reference = @newReference, IsUserOverriden = 1 " +
                        "WHERE Reference = @oldReference";
                    replaceReferences.Parameters.AddWithValue("@newReference", newReference);
                    replaceReferences.Parameters.AddWithValue("@oldReference", oldReference);
                    await replaceReferences.ExecuteNonQueryAsync();
                }
            }
            catch (Exception)
            {
                Console.WriteLine("❌ Could not replace references");
                throw;
            }
            finally
            {
                await sqlite.CloseAsync();
            }
        }

    }
}
