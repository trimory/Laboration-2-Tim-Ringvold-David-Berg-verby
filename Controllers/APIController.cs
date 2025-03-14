using Laboration2MVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Laboration2MVC.Controllers
{
    public class APIController : Controller
    {
        string jsonResult = string.Empty;
        static readonly HttpClient client = new HttpClient();
     
        public IActionResult Index()
        {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer 3821dadd2be2c2d1dca7da9381f8bd91a1e9421e");
            using (HttpResponseMessage response = client.GetAsync("https://bank.stuxberg.se/api/iban/SE4550000000058398257466/").Result)
            {
                using (HttpContent content = response.Content)
                {
                    jsonResult = content.ReadAsStringAsync().Result;
                    var jsonResultDeserialized = JsonSerializer.Deserialize<List<APIModel>>(jsonResult);

                    ViewData["apiResult"] = jsonResultDeserialized;
                }
            }
            return View();

        }
    }
}
