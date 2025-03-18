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

       async public Task DeleteAndUpdateDatabase()
        {
            try
            {
                if (File.Exists(databaseFilePath))
                {
                    File.Delete(databaseFilePath);
                    Console.WriteLine("Database deleted");
                }
                else Console.WriteLine("Database does not exist");
            }
            catch (Exception)
            {
                Console.WriteLine("error deleting database");
                throw;
            }
        }
        async public Task CreateDatabase()
        {
            try
            {
                if (!File.Exists(databaseFilePath))
                {
                    
                    await sqlite.OpenAsync();
                    using var createTransactionTable = sqlite.CreateCommand();
                    createTransactionTable.CommandText = @"CREATE TABLE IF NOT EXISTS transactions " +
                        "(TransactionID INTEGER PRIMARY KEY NOT NULL," +
                        " bookingDate TEXT, transactionDate TEXT NOT NULL," +
                        " Reference TEXT, Amount REAL, Balance REAL NOT NULL, " +
                        "Category TEXT, " +
                        "UserOverwrittenCategory BIT)";
                    await createTransactionTable.ExecuteNonQueryAsync();


                    using var CreateCustomCategory = sqlite.CreateCommand();
                    CreateCustomCategory.CommandText = @"
                    CREATE TABLE IF NOT EXISTS CustomCategory 
                    (
                        OriginalCategory TEXT NOT NULL UNIQUE,
                        NewCategory TEXT NOT NULL
                    )";

                    await CreateCustomCategory.ExecuteNonQueryAsync();

                    using var CreateTransactionCategory = sqlite.CreateCommand();
                    CreateTransactionCategory.CommandText = @"
                    CREATE TABLE IF NOT EXISTS TransactionCategory 
                    (
                        TransactionID INTEGER PRIMARY KEY NOT NULL,
                        Category TEXT NOT NULL
                    )";
                    await CreateTransactionCategory.ExecuteNonQueryAsync();

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

        async public Task<List<TransactionCategoryModel>> GetTransactionRules()
        {
            List<TransactionCategoryModel> customCategories = new List<TransactionCategoryModel>();
            try
            {
                await sqlite.OpenAsync();
                using var getCustomCategories = sqlite.CreateCommand();
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
                await sqlite.CloseAsync();
            }
            return customCategories;
        }

        public async Task ApplyCustomRulesToTransactions()
        {

            //updates transaction categories based on custom rules

            var CustomCategoryRules = await GetCustomRules();
            
            try
            {
                await sqlite.OpenAsync();

                foreach (var rule in CustomCategoryRules)
                {
                    foreach (var oldReference in rule.OriginalCategories)
                    {
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
                Console.WriteLine(" applied custom rules to transactions.");
            }
            catch (Exception)
            {
                Console.WriteLine("Could not apply custom rules to transactions.´");
                throw;
            }

            //updates individual transaction overrides
            var TransactionCategoryRules = await GetTransactionRules();
            try
            {
                await sqlite.OpenAsync();
                foreach (var rule in TransactionCategoryRules)
                {
                    using var command = sqlite.CreateCommand();
                    command.CommandText = @"
                        UPDATE transactions
                        SET Category = @newCategory, UserOverwrittenCategory = 1
                        WHERE TransactionID = @transactionID";
                    command.Parameters.AddWithValue("@newCategory", rule.Category);
                    command.Parameters.AddWithValue("@transactionID", rule.TransactionID);
                    await command.ExecuteNonQueryAsync();
                }
                Console.WriteLine("Successfully applied custom rules to transactions.");
            }
            catch (Exception)
            {
                Console.WriteLine( "Could not apply custom rules to transactions. ");
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

        async public Task CreateCustomCategoryTransactionID(int TransactionID, string newCategory) 
        {
            if (TransactionID == 0)
            {
                throw new ArgumentException("TransactionID cannot be null or empty.");
            }
            if (string.IsNullOrEmpty(newCategory))
            {
                throw new ArgumentException("newCategory cannot be null or empty.");
            }

            using var connection = new SqliteConnection($"Data Source=userTransactions.db");
            try
            {
                await connection.OpenAsync();


               
                using var UpdateTransaction = connection.CreateCommand();
                UpdateTransaction.CommandText = @"
                    INSERT OR IGNORE INTO TransactionCategory (TransactionID, Category)
                    VALUES (@TransactionID, @Category);
                    UPDATE TransactionCategory
                    SET NewCategory = @Category
                    WHERE TransactionID = @TransactionID";
                UpdateTransaction.Parameters.AddWithValue("@TransactionID", TransactionID);
                UpdateTransaction.Parameters.AddWithValue("@Category", newCategory);
                await UpdateTransaction.ExecuteNonQueryAsync();
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to update custom transaction categories");
                throw;
            }
            finally
            {
                await sqlite.CloseAsync();
            }
        }


    }
}
