using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Data.Sqlite;
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

        public async Task<bool> IsDatabaseEmpty()
        {
            try
            {
                await sqlite.OpenAsync();
                using var checkCount = sqlite.CreateCommand();
                checkCount.CommandText = "SELECT COUNT(*) FROM transactions"; 

                var count = (long)await checkCount.ExecuteScalarAsync();
                return count == 0; 
            }
            catch (Exception ex)
            {
                Console.WriteLine("error");
                return true; 
            }
            finally
            {
                await sqlite.CloseAsync();
            }
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
                        "IsUserReferenceChanged BIT NOT NULL DEFAULT 0)";
                    await createTable.ExecuteNonQueryAsync();
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
        async public Task CreateCustomCategory(string originalCategory, string newCategory)
        {
            try
            {
                await sqlite.OpenAsync();

                using var createCustomCategory = sqlite.CreateCommand();
                createCustomCategory.CommandText = @"
            CREATE TABLE IF NOT EXISTS CustomCategory 
            (
                OriginalCategory TEXT NOT NULL,
                NewCategory TEXT NOT NULL,
                UNIQUE (OriginalCategory, NewCategory)
            )";
                //initiate new table if none exists yet

                await createCustomCategory.ExecuteNonQueryAsync();

                using var checkCategory = sqlite.CreateCommand();
                checkCategory.CommandText = @"
                SELECT COUNT(*) FROM CustomCategory WHERE OriginalCategory = 
                @OriginalCategory AND NewCategory = @NewCategory";

                checkCategory.Parameters.AddWithValue("@OriginalCategory", originalCategory);
                checkCategory.Parameters.AddWithValue("@NewCategory", newCategory);

                //check if category already exists
                var existingCount = (long)await checkCategory.ExecuteScalarAsync();

                if (existingCount == 0)
                {
                    using var insertCategory = sqlite.CreateCommand();
                    insertCategory.CommandText = @"
                INSERT INTO CustomCategory (OriginalCategory, NewCategory) 
                VALUES (@OriginalCategory, @NewCategory)";

                    insertCategory.Parameters.AddWithValue("@OriginalCategory", originalCategory);
                    insertCategory.Parameters.AddWithValue("@NewCategory", newCategory);

                    await insertCategory.ExecuteNonQueryAsync();
                }
                
            }
            catch (Exception)
            {
                Console.WriteLine($"error, could not write");
                throw;
            }
            finally
            {
                await sqlite.CloseAsync();
            }
        }

        public async Task InsertTransaction(TransactionModel transaction)
        {
            try
            {

                // Merge this with create database
                await sqlite.OpenAsync();
                using var insertTransaction = sqlite.CreateCommand();
                insertTransaction.CommandText = @"INSERT INTO transactions 
                (bookingDate, transactionDate, Reference, Amount, Balance, IsUserReferenceChanged) " +
                    "VALUES (@bookingDate, @transactionDate, @Reference, @Amount, @Balance, @IsUserReferenceChanged)";
                insertTransaction.Parameters.AddWithValue("@bookingDate", transaction.BookingDate);
                insertTransaction.Parameters.AddWithValue("@transactionDate", transaction.TransactionDate);
                insertTransaction.Parameters.AddWithValue("@Reference", transaction.Reference);
                insertTransaction.Parameters.AddWithValue("@Amount", transaction.Amount);
                insertTransaction.Parameters.AddWithValue("@Balance", transaction.Balance);
                insertTransaction.Parameters.AddWithValue("@IsUserReferenceChanged", transaction.IsUserReferenceChanged ? 1 : 0); //stores true and false values as bit 1 and 0
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
                        IsUserReferenceChanged = reader.GetInt32(6) == 1 //converts 1 to true, 0 to false as this value is stored as a bit

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
        public async Task ReplaceReferences(string oldReference, string newReference)
        {
            try
            {
                await sqlite.OpenAsync();
                using var replaceReferences = sqlite.CreateCommand();
                replaceReferences.CommandText = "UPDATE transactions " +
                    "SET Reference = @newReference, IsUserReferenceChanged = 1 " + 
                    "WHERE Reference = @oldReference"; 
                replaceReferences.Parameters.AddWithValue("@newReference", newReference);
                replaceReferences.Parameters.AddWithValue("@oldReference", oldReference);
                await replaceReferences.ExecuteNonQueryAsync();
            }
            catch (Exception)
            {
                Console.WriteLine("could not replace references");
                throw;
            }
            finally
            {
                await sqlite.CloseAsync();
            }
        }
    }
}
