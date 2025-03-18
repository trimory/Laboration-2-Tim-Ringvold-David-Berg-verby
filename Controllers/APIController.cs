using Laboration2MVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;

namespace Laboration2MVC.Controllers
{
    public class APIController : Controller
    {

        private readonly DatabaseModel dbModel;
        string jsonResult = string.Empty;


        //default rules using the custom category table
        public async Task ApplyDefaultRules()
        {
            await dbModel.CreateCustomCategory(new List<string> { "ICA MAT","Mathem","Maxi ICA", "Hemköp mat" }, "Livsmedel");
            await dbModel.CreateCustomCategory(new List<string> { "NETFLIX", "PayEx", "Spotify", "Vattenfall" }, "Nätbetalningar");
            await dbModel.CreateCustomCategory(new List<string> { "APOTEKET AB", "Apoteket/PayEx", "Sjukresa" }, "Sjukvård");
            await dbModel.CreateCustomCategory(new List<string> { "Uttag", "Fk/pmynd" }, "Inkomster och uttag");

        }
        public APIController(DatabaseModel importDbModel)
        {
            dbModel = importDbModel;
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAndRedirect()
        {
            await dbModel.DeleteCustomRules();
            TempData["Message"] = "Database deleted";
            await ApplyDefaultRules();

            //  returns to transaction view which contains logic for api retrieval
            return RedirectToAction("TransactionView");
        }

        
        public async Task<IActionResult> TransactionView()
        {
            List<TransactionModel> transactions;
            if (System.IO.File.Exists(dbModel.databaseFilePath))
            {
                // if the DB file exists, check if it contains data
                bool isEmpty = await dbModel.CheckIfDatabaseEmpty();
                if (!isEmpty)
                {
                    // Database has data—use it.
                    transactions = await dbModel.GetTransactions();
                    await dbModel.ApplyCustomRulesToTransactions();
                    // Reload transactions to get the updated categories
                    transactions = await dbModel.GetTransactions();
                }
                else
                {
                    // File exists but DB is empty—fetch from API.
                    transactions = await FetchTransactionsFromAPI();
                    await dbModel.CreateDatabase();
                    foreach (var transaction in transactions)
                    {
                        await dbModel.InsertTransaction(transaction);
                    }
                }
            }
            else
            {
                // Database file does not exist—fetch from API.
                transactions = await FetchTransactionsFromAPI();
                await dbModel.CreateDatabase();
                foreach (var transaction in transactions)
                {
                    await dbModel.InsertTransaction(transaction);
                }
            }

            ViewData["apiResult"] = transactions;
            return View();
        }

        private async Task<List<TransactionModel>> FetchTransactionsFromAPI()
        {
            HttpClient client = new HttpClient();
            if (!client.DefaultRequestHeaders.Contains("Authorization"))
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer 3821dadd2be2c2d1dca7da9381f8bd91a1e9421e");
            }
            using (HttpResponseMessage response = await client.GetAsync("https://bank.stuxberg.se/api/iban/SE4550000000058398257466/"))
            {
                string jsonResult = await response.Content.ReadAsStringAsync();
                var transactions = JsonSerializer.Deserialize<List<TransactionModel>>(jsonResult);
                return transactions;
            }
        }
        public async Task<IActionResult> UpdateTransactionsFromAPI()
        {
            List<TransactionModel> transactions = await FetchTransactionsFromAPI();
            var dbTransactions = await dbModel.GetTransactions();
            var existingIds = dbTransactions.Select(t => t.TransactionID).ToList();

            foreach(var transaction in transactions)
            {
                if (!existingIds.Contains(transaction.TransactionID))
                {
                    await dbModel.InsertTransaction(transaction);
                }
            }
            await ApplyDefaultRules();
            transactions = await dbModel.GetTransactions();
            ViewData["apiResult"] = transactions;

            return RedirectToAction("TransactionView");

        }
        public IActionResult ShowError(string message)
        {
            var errorModel = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                Message = message
            };

            return View("Error", errorModel);
        }
    }
}
