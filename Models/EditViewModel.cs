namespace Laboration2MVC.Models
{
    public class EditViewModel
    {
        public TransactionModel Transaction { get; set; } = new TransactionModel();
        public CustomCategoryModel CustomCategory { get; set; } = new CustomCategoryModel();
        public List<ReferenceModel> ReferenceList { get; set; } = new List<ReferenceModel>();

        public List<string> SelectedCategories { get; set; } = new List<string>();

        public TransactionCategoryModel TransactionCategory { get; set; } = new TransactionCategoryModel();

        public List<TransactionModel> Transactions { get; set; } = new(); 



    }
}