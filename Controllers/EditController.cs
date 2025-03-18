using Laboration2MVC.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Laboration2MVC.Controllers
{
    public class EditController : Controller
    {
        private readonly DatabaseModel dbModel;

        public EditController(DatabaseModel importDbModel)
        {
            dbModel = importDbModel;
        }

        [HttpGet]
        public async Task<IActionResult> EditView()
        {
            var model = new EditViewModel
            {
                ReferenceList = await dbModel.GetUniqueReferences() // ✅ Fetch unique references
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditView(EditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Invalid input. Please try again.";
                model.ReferenceList = await dbModel.GetUniqueReferences(); // ✅ Reload options if form fails
                return View(model);
            }

            try
            {
                Console.WriteLine($"Received form data: {string.Join(", ", model.CustomCategory.OriginalCategories)} -> {model.CustomCategory.NewCategory}");

                await dbModel.CreateCustomCategory(model.CustomCategory.OriginalCategories, model.CustomCategory.NewCategory);
                TempData["Message"] = $"✅ Categories mapped to '{model.CustomCategory.NewCategory}' successfully!";

                await dbModel.ApplyCustomRulesToTransactions(); //Applies custom rules to server
                model.SelectedCategories = model.CustomCategory.OriginalCategories;
            }
            catch
            {
                TempData["Message"] = "❌ Error: Could not create category mappings.";
            }

            model.ReferenceList = await dbModel.GetUniqueReferences(); // ✅ Ensure dropdown options reload
            return View(model);
        }
    }
}