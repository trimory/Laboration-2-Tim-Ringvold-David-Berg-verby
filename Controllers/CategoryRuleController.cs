using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Laboration2_MVC.Models;
public class CategoryRuleController : Controller
{
    private readonly TransactionDbContext _context;

    public CategoryRuleController(TransactionDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        // Debug information about what's actually in the database
        var categoryCount = _context.Categories.Count();
        var ruleCount = _context.CategoryRules.Count();

        Console.WriteLine($"Database has {categoryCount} categories and {ruleCount} rules");

        var rules = await _context.CategoryRules.Include(r => r.Category).ToListAsync();

        // Display the count in TempData for the view
        TempData["Debug"] = $"Found {rules.Count} rules in database. Database has {categoryCount} categories.";

        return View(rules);
    }

    [HttpGet]
    public IActionResult Create()
    {
        ViewBag.Categories = _context.Categories.ToList();

        // Get distinct transaction references
        var uniqueReferences = _context.Transactions
            .Select(t => t.Reference)
            .Distinct()
            .OrderBy(r => r)
            .ToList();

        ViewBag.UniqueReferences = uniqueReferences;
        return View();
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryRule rule)
    {
        // Debug information 
        Console.WriteLine($"Received rule: Keyword={rule.Keyword}, CategoryID={rule.CategoryID}");

        if (ModelState.IsValid)
        {
            try
            {
                // First check if the category exists
                var category = await _context.Categories.FindAsync(rule.CategoryID);
                if (category == null)
                {
                    ModelState.AddModelError("CategoryID", "Den valda kategorin finns inte.");
                    Console.WriteLine($"Error: Category with ID {rule.CategoryID} not found");
                }
                else
                {
                    _context.CategoryRules.Add(rule);
                    var result = await _context.SaveChangesAsync();
                    Console.WriteLine($"SaveChanges result: {result} entities saved");

                    // Verify the rule was saved
                    var savedRule = await _context.CategoryRules
                        .FirstOrDefaultAsync(r => r.Keyword == rule.Keyword && r.CategoryID == rule.CategoryID);

                    if (savedRule != null)
                    {
                        Console.WriteLine($"Rule saved with ID: {savedRule.RuleID}");
                        TempData["SuccessMessage"] = "Kategoriregel har skapats! Gå till Transaktioner och klicka på 'Uppdatera Transaktioner' för att applicera reglerna.";
                        return RedirectToAction(nameof(Index));
                    }
                    else
                    {
                        Console.WriteLine("Rule not found after save!");
                        ModelState.AddModelError("", "Regeln sparades men kunde inte hittas efteråt.");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error saving rule: {ex.Message}");
                Console.WriteLine($"Inner exception: {ex.InnerException?.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                ModelState.AddModelError("", "Ett fel uppstod när regeln skulle sparas: " + ex.Message);
            }
        }
        else
        {
            // Log model state errors
            foreach (var state in ModelState)
            {
                if (state.Value.Errors.Any())
                {
                    Console.WriteLine($"Error in {state.Key}: {string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage))}");
                }
            }
        }

        // Always repopulate ViewBag data
        ViewBag.Categories = _context.Categories.ToList();
        var uniqueReferences = _context.Transactions
            .Select(t => t.Reference)
            .Distinct()
            .OrderBy(r => r)
            .ToList();
        ViewBag.UniqueReferences = uniqueReferences;

        return View(rule);
    }
}