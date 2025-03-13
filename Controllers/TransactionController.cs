using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Laboration2_MVC.Models;
using Microsoft.EntityFrameworkCore;

public class TransactionController : Controller
{
    private readonly TransactionDbContext _context;
    private readonly HttpClient _httpClient;

    public TransactionController(TransactionDbContext context)
    {
        _context = context;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer 3821dadd2be2c2d1dca7da9381f8bd91a1e9421e");
    }

    // Action method to display the list of transactions
    public async Task<IActionResult> Index()
    {
        // Check if there are any transactions in the database
        if (_context.Transactions.Any())
        {
            // If transactions exist, return the view with the list of transactions
            return View("~/Views/Transactions/Index.cshtml", await _context.Transactions.ToListAsync());
        }

        // API URL to fetch transactions
        string apiUrl = "https://bank.stuxberg.se/api/iban/SE4550000000058398257466/";
        var response = await _httpClient.GetAsync(apiUrl);

        // Check if the API call was successful
        if (!response.IsSuccessStatusCode)
        {
            ViewBag.ErrorMessage = "Kunde inte hämta data från API:et.";
            return View("~/Views/Transactions/Index.cshtml", new List<Transaction>());
        }

        // Deserialize the JSON response to a list of transactions
        var json = await response.Content.ReadAsStringAsync();
        var transactions = JsonSerializer.Deserialize<List<Transaction>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Apply category rules to the transactions
        var categoryRules = _context.CategoryRules.ToList();
        foreach (var transaction in transactions)
        {
            foreach (var rule in categoryRules)
            {
                if (transaction.Reference.Contains(rule.Keyword, StringComparison.OrdinalIgnoreCase))
                {
                    transaction.Category = rule.Category;
                    break;
                }
            }
        }

        // Add the transactions to the database and save changes
        _context.Transactions.AddRange(transactions);
        await _context.SaveChangesAsync();

        // Return the view with the list of transactions
        return View("~/Views/Transactions/Index.cshtml", transactions);
    }

    // GET: Edit transaction by ID
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var transaction = await _context.Transactions.FindAsync(id);
        if (transaction == null)
            return NotFound();
        return View("~/Views/Transactions/Edit.cshtml", transaction);
    }

    // POST: Edit transaction
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("TransactionID,BookingDate,Reference,Amount,Category")] Transaction transaction)
    {
        if (id != transaction.TransactionID)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Update the transaction in the database
                _context.Update(transaction);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Ändringar sparades!";
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Transactions.Any(e => e.TransactionID == transaction.TransactionID))
                {
                    return NotFound();
                }
                else
                {
                    TempData["ErrorMessage"] = "Ett fel uppstod vid uppdatering.";
                }
            }
            // Redirect to the list of transactions
            return RedirectToAction(nameof(Index));
        }
        // If there are errors, reload the view
        return View(transaction);
    }
}
