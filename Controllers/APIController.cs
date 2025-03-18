using Laboration2MVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Laboration2MVC.Controllers
{
    public class APIController : Controller
    {

        private readonly DatabaseModel dbModel;
        string jsonResult = string.Empty;


        public APIController(DatabaseModel importDbModel)
        {
            dbModel = importDbModel;
        }

        public async Task<IActionResult> TransactionView()
        {

            if(System.IO.File.Exists(dbModel.databaseFilePath))
            {
                var dbResult = await dbModel.GetTransactions();
                ViewData["apiResult"] = dbResult;
            }

            else {
                HttpClient client = new HttpClient();

                if (!client.DefaultRequestHeaders.Contains("Authorization"))
                {
                    client.DefaultRequestHeaders.Add("Authorization", "Bearer 3821dadd2be2c2d1dca7da9381f8bd91a1e9421e");
                }

                using (HttpResponseMessage response = await client.GetAsync("https://bank.stuxberg.se/api/iban/SE4550000000058398257466/"))
                {
                    response.EnsureSuccessStatusCode();
                    using (HttpContent content = response.Content)
                    {
                        jsonResult = await content.ReadAsStringAsync();

                        var jsonResultDeserialized = JsonSerializer.Deserialize<List<TransactionModel>>(jsonResult);

                        if (!System.IO.File.Exists(dbModel.databaseFilePath))
                        {

                            await dbModel.CreateDatabase();

                            foreach (var transaction in jsonResultDeserialized)
                            {
                                await dbModel.InsertTransaction(transaction);
                            }
                        }
                        ViewData["apiResult"] = jsonResultDeserialized;
                    }
                }
            }
            
            
            return View();
        }
    }
}
