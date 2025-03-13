using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

public class TransactionController : Controller
{
    private readonly HttpClient _httpClient;

    public TransactionController()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer 3821dadd2be2c2d1dca7da9381f8bd91a1e9421e");
    }

    public async Task<IActionResult> Index()
    {
        string apiUrl = "https://bank.stuxberg.se/api/iban/SE4550000000058398257466/";
        var transactions = new List<Transaction>();

        try
        {
            var response = await _httpClient.GetAsync(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                
                // Använd System.Text.Json för snabbare deserialisering
                transactions = JsonSerializer.Deserialize<List<Transaction>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            else
            {
                ViewBag.ErrorMessage = "Kunde inte hämta data från API:et.";
            }
        }
        catch (Exception ex)
        {
            ViewBag.ErrorMessage = $"Fel vid hämtning av data: {ex.Message}";
        }

        return View("~/Views/Home/TransactionView.cshtml", transactions);
    }
}
