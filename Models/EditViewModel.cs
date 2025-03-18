using Microsoft.AspNetCore.Mvc;

namespace Laboration2MVC.Models
{
    public class EditViewModel 
    {
        public TransactionModel Transaction { get; set; } = new TransactionModel();

        public CustomCategoryModel CustomCategory { get; set; } = new CustomCategoryModel();

        public List<ReferenceModel> ReferenceList { get; set; } = new List<ReferenceModel>(); 


    }
}
