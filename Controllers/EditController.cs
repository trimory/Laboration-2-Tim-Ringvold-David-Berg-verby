using Laboration2MVC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Laboration2MVC.Controllers
{
    public class EditController : Controller
    {
        private readonly DatabaseModel _databaseModel;

        // ✅ Inject DatabaseModel via Dependency Injection
        public EditController(DatabaseModel databaseModel)
        {
            _databaseModel = databaseModel;
        }

        // ✅ Displays a form for category creation
        public ActionResult Create()
        {
            return View();
        }

        // ✅ Handles category creation (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string originalCategory, string newCategory)
        {
            if (string.IsNullOrWhiteSpace(originalCategory) || string.IsNullOrWhiteSpace(newCategory))
            {
                TempData["Message"] = "❌ Both category fields are required!";
                return RedirectToAction(nameof(Create));
            }

            try
            {
                await _databaseModel.CreateCustomCategory(originalCategory, newCategory);
                TempData["Message"] = $"✅ Category '{originalCategory}' mapped to '{newCategory}' successfully!";
            }
            catch
            {
                TempData["Message"] = "❌ Error: Could not create category mapping.";
            }

            return RedirectToAction(nameof(Create));
        }

        // ✅ Displays Edit View
        public ActionResult EditView()
        {
            return View();
        }
    }
}