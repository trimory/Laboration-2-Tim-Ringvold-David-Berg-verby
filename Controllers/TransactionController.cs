using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using Laboration2_MVC.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text;

public class TransactionsController : Controller
{
    private readonly TransactionDbContext _context;
    private readonly HttpClient _httpClient;

    public TransactionsController(TransactionDbContext context)
    {
        _context = context;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer 3821dadd2be2c2d1dca7da9381f8bd91a1e9421e");
    }

    // GET: Transactions - Hämta alla transaktioner och uppdatera nya
    public async Task<IActionResult> Index()
    {
        string apiUrl = "https://bank.stuxberg.se/api/iban/SE4550000000058398257466/";
        var response = await _httpClient.GetAsync(apiUrl);

        if (!response.IsSuccessStatusCode)
        {
            ViewBag.ErrorMessage = "Kunde inte hämta data från API:et.";
            return View("~/Views/Transactions/Index.cshtml", await _context.Transactions.ToListAsync());
        }

        var json = await response.Content.ReadAsStringAsync();
        var apiTransactions = JsonSerializer.Deserialize<List<Transaction>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        var existingTransactionIds = _context.Transactions.Select(t => t.TransactionID).ToHashSet();
        var newTransactions = apiTransactions
            .Where(t => !existingTransactionIds.Contains(t.TransactionID))
            .ToList();

        if (newTransactions.Any())
        {
            var categoryRules = _context.CategoryRules.Include(r => r.Category).ToList();

            foreach (var transaction in newTransactions)
            {
                transaction.Category = "Övrigt";  // Standardkategori

                foreach (var rule in categoryRules)
                {
                    if (transaction.Reference.Contains(rule.Keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        transaction.Category = rule.Category.Name; // Nu kopplas det till en riktig kategori
                        break;
                    }
                }
            }

            _context.Transactions.AddRange(newTransactions);
            await _context.SaveChangesAsync();
        }

        return View("~/Views/Transactions/Index.cshtml", await _context.Transactions.ToListAsync());
    }



    // GET: Transactions/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var transaction = await _context.Transactions.FirstOrDefaultAsync(m => m.TransactionID == id);
        if (transaction == null)
        {
            return NotFound();
        }
        return View("~/Views/Transactions/Details.cshtml", transaction);
    }

    // GET: Transactions/Create
    public IActionResult Create()
    {
        return View("~/Views/Transactions/Create.cshtml");
    }

    // POST: Transactions/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("TransactionID,BookingDate,TransactionDate,Reference,Amount,Balance,Category")] Transaction transaction)
    {
        if (ModelState.IsValid)
        {
            _context.Add(transaction);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View("~/Views/Transactions/Create.cshtml", transaction);
    }

    // GET: Transactions/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var transaction = await _context.Transactions.FindAsync(id);
        if (transaction == null)
            return NotFound();
        return View("~/Views/Transactions/Edit.cshtml", transaction);
    }

    // POST: Transactions/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("TransactionID,BookingDate,TransactionDate,Reference,Amount,Balance,Category")] Transaction transaction)
    {
        if (id != transaction.TransactionID)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                // Hämta den ursprungliga transaktionen från databasen
                var originalTransaction = await _context.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.TransactionID == id);
                if (originalTransaction == null)
                {
                    return NotFound();
                }

                // Behåll de ursprungliga värdena för fälten som inte ska ändras
                transaction.BookingDate = originalTransaction.BookingDate;
                transaction.TransactionDate = originalTransaction.TransactionDate;
                transaction.Reference = originalTransaction.Reference;
                transaction.Amount = originalTransaction.Amount;
                transaction.Balance = originalTransaction.Balance;

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
            return RedirectToAction(nameof(Index));
        }
        return View("~/Views/Transactions/Edit.cshtml", transaction);
    }



    // GET: Transactions/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var transaction = await _context.Transactions.FindAsync(id);
        if (transaction == null)
        {
            return NotFound();
        }
        return View("~/Views/Transactions/Delete.cshtml", transaction);
    }

    // POST: Transactions/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var transaction = await _context.Transactions.FindAsync(id);
        if (transaction != null)
        {
            _context.Transactions.Remove(transaction);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }
    [HttpGet]
    public async Task<IActionResult> MassEdit()
    {
        return View(await _context.Transactions.ToListAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MassEdit(List<Transaction> transactions)
    {
        if (ModelState.IsValid)
        {
            _context.UpdateRange(transactions);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Ändringar sparades!";
            return RedirectToAction(nameof(Index));
        }
        return View("~/Views/Transactions/MassEdit.cshtml", transactions);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshTransactions()
    {
        StringBuilder debugInfo = new StringBuilder();

        // Radera befintliga transaktioner
        _context.Transactions.RemoveRange(_context.Transactions);
        await _context.SaveChangesAsync();

        // Ladda ner nya transaktioner från API:et
        string apiUrl = "https://bank.stuxberg.se/api/iban/SE4550000000058398257466/";
        var response = await _httpClient.GetAsync(apiUrl);

        if (!response.IsSuccessStatusCode)
        {
            TempData["ErrorMessage"] = "Kunde inte hämta data från API:et.";
            return RedirectToAction(nameof(Index));
        }

        var json = await response.Content.ReadAsStringAsync();
        var apiTransactions = JsonSerializer.Deserialize<List<Transaction>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Debug information
        var categoryRules = _context.CategoryRules.Include(r => r.Category).ToList();
        debugInfo.AppendLine($"Found {categoryRules.Count} category rules:");

        // List all rules for debugging
        foreach (var rule in categoryRules)
        {
            debugInfo.AppendLine($"Rule: '{rule.Keyword}' -> Category: '{rule.Category?.Name ?? "null"}' (CategoryID: {rule.CategoryID})");
        }

        debugInfo.AppendLine("\nTransactions processing:");
        int matchCount = 0;

        foreach (var transaction in apiTransactions)
        {
            transaction.Category = "Övrigt"; // Standardkategori
            string originalRef = transaction.Reference;

            foreach (var rule in categoryRules)
            {
                if (rule.Category == null)
                {
                    debugInfo.AppendLine($"WARNING: Rule {rule.RuleID} with keyword '{rule.Keyword}' has null Category!");
                    continue;
                }

                if (originalRef.Contains(rule.Keyword, StringComparison.OrdinalIgnoreCase))
                {
                    transaction.Category = rule.Category.Name;
                    matchCount++;
                    debugInfo.AppendLine($"Match: '{originalRef}' with rule '{rule.Keyword}' -> '{rule.Category.Name}'");
                    break;
                }
            }
        }

        debugInfo.AppendLine($"\nTotal matches: {matchCount} out of {apiTransactions.Count} transactions");

        _context.Transactions.AddRange(apiTransactions);
        await _context.SaveChangesAsync();

        TempData["Debug"] = debugInfo.ToString();
        TempData["SuccessMessage"] = "Transaktioner uppdaterades!";
        return RedirectToAction(nameof(Index));
    }



}
