using Microsoft.AspNetCore.Mvc;

namespace Laboration2MVC.Models
{
    public class CustomCategoryModel 
    {
        //fix unnecessary public access modifiers

        public List<string> OriginalCategories { get; set; } = new List<string>();
        public string NewCategory { get; set; } = string.Empty;
    }
    
}
