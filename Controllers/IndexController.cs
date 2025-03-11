using Microsoft.AspNetCore.Mvc;

namespace Laboration_2_Tim_Ringvold__David_Berg_Överby.Controllers
{
    public class IndexController : Controller
    {
        string jsonResult = string.Empty;
        HttpClient client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Add("Authorization", "Bearer Token");
        public IActionResult Index()
        {
            return View();

        }
    }
}
