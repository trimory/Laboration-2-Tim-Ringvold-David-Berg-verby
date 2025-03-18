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
                ReferenceList = await dbModel.GetUniqueReferences(),
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> EditTransactionView()
        {
            Console.WriteLine("👋 GET EditTransactionView called");

            var model = new EditViewModel
            {
                Transactions = await dbModel.GetTransactions(),
                ReferenceList = await dbModel.GetUniqueReferences()
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
                model.ReferenceList = await dbModel.GetUniqueReferences(); 
                return View(model);
            }

            try
            {

                //creates a custom category in the custom category table
                await dbModel.CreateCustomCategory(model.CustomCategory.OriginalCategories, model.CustomCategory.NewCategory);

                await dbModel.ApplyCustomRulesToTransactions(); //Applies custom rules to transactions database ca
                model.SelectedCategories = model.CustomCategory.OriginalCategories;
            }
            catch
            {
                TempData["Message"] = "Could not create referen";
            }

            model.ReferenceList = await dbModel.GetUniqueReferences(); // Ensure dropdown options reload
            return View(model);
        }

       

        public async Task<IActionResult> EditTransactionView(EditViewModel model)
        {

            if (!ModelState.IsValid)
            {
                TempData["Message"] = "Invalid input. Please try again.";

                model.Transactions = await dbModel.GetTransactions();
                model.TransactionCategory.Category = string.Empty;
                return View(model);
            }

            try
            {
                await dbModel.CreateCustomCategoryTransactionID(model.TransactionCategory.TransactionID, model.TransactionCategory.Category);

                await dbModel.ApplyCustomRulesToTransactions();
            }
            catch
            {
                TempData["Message"] = "Could not create Custom Rules";
            }

            model.Transactions = await dbModel.GetTransactions();
            model.TransactionCategory.Category = string.Empty;

            return View("EditTransactionView", model);
        }




    }
}