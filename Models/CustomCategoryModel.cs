using Microsoft.AspNetCore.Mvc;

namespace Laboration2MVC.Models
{
    public class CustomCategoryModel 
    {
        //fix unnecessary public access modifiers

        public string OriginalCategory { get; set; } = string.Empty;
        public string NewCategory { get; set; } = string.Empty;
    }
}
