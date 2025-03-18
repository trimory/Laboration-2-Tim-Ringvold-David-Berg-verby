using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using Laboration2_MVC.Models;

namespace Laboration2_MVC.Controllers
{
    public class CategoryController : Controller
    {
        private readonly TransactionDbContext _context;

        public CategoryController(TransactionDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View(_context.Categories.ToList());
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Kategori har skapats!";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }
    }
}
