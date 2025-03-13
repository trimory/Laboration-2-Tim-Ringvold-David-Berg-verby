using System.ComponentModel.DataAnnotations;

namespace Laboration2_MVC.Models
{
    public class CategoryRule
    {
        [Key]
        public int RuleID { get; set; }
        public string Keyword { get; set; }  // Ex: "Willys" ska bli "Mat"
        public string Category { get; set; }  // Ex: "Mat"
    }
}
