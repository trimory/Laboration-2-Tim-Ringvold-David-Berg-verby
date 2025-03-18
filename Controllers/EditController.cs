using Laboration2MVC.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Laboration2MVC.Controllers
{
    public class EditController : Controller
    {
        private readonly DatabaseModel dbModel;

        // ✅ Inject DatabaseModel via Dependency Injection
        public EditController(DatabaseModel importDbModel)
        {
            dbModel = importDbModel;
        }

        [HttpGet]
        public async Task<IActionResult> EditView()
        {
            var model = new EditViewModel
            {
                ReferenceList = await dbModel.GetUniqueReferences() 
            }; 

            return View(model);
        }


        // ✅ Handles category creation (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditView(EditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Invalid input. Please try again.";
                return View(model); // Return the view with validation errors
            }

            try
            {
                Console.WriteLine($"Received form data: {model.CustomCategory.OriginalCategory} -> {model.CustomCategory.OriginalCategory}");

                await dbModel.CreateCustomCategory(model.CustomCategory.OriginalCategory, model.CustomCategory.NewCategory);
                TempData["Message"] = $"✅ Category '{model.CustomCategory.OriginalCategory}' mapped to '{model.CustomCategory.NewCategory}' successfully!";
                await dbModel.ReplaceReferences(model.CustomCategory.OriginalCategory, model.CustomCategory.NewCategory);
            }
            catch
            {
                TempData["Message"] = "❌ Error: Could not create category mapping.";
            }

            return RedirectToAction(nameof(EditView)); 
        }

        
    }
}