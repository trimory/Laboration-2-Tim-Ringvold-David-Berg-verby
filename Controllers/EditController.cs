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
            try
            {
                var model = new EditViewModel
                {
                    ReferenceList = await dbModel.GetUniqueReferences(),
                };
                return View(model);

            }
            catch
            {
                TempData["CatchError"] = "Could not access database, refresh";
                return View();
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditTransactionView()
        {

            try
            {
                var model = new EditViewModel
                {
                    Transactions = await dbModel.GetTransactions(),
                    ReferenceList = await dbModel.GetUniqueReferences()
                };

                return View(model);
            }
            catch
            {
                TempData["CatchError"] = "Could not access database, refresh";
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditView(EditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["CatchError"] = "Invalid input. Please try again.";
                model.ReferenceList = await dbModel.GetUniqueReferences(); 
                return View(model);
            }

            try
            {

                //creates a custom category in the custom category table
                await dbModel.CreateCustomCategory(model.CustomCategory.OriginalCategories, model.CustomCategory.NewCategory);

                await dbModel.ApplyCustomRulesToTransactions(); //Applies custom rules to transactions database 
                model.SelectedCategories = model.CustomCategory.OriginalCategories;
            }
            catch
            {
                TempData["CatchError"] = "Database write error";
            }

            model.ReferenceList = await dbModel.GetUniqueReferences(); // Ensure dropdown options reload
            return View(model);
        }

       

        public async Task<IActionResult> EditTransactionView(EditViewModel model)
        {

            if (!ModelState.IsValid)
            {
                TempData["CatchError"] = "Invalid input. Please try again.";

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
                TempData["CatchError"] = "Could not create custom rules, data base error";

                model.TransactionCategory.Category = string.Empty;
                return View(model);

            }

            model.Transactions = await dbModel.GetTransactions();
            model.TransactionCategory.Category = string.Empty;

            return View("EditTransactionView", model);
        }




    }
}