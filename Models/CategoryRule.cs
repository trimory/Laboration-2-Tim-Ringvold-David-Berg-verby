using Laboration2_MVC.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class CategoryRule
{
    [Key]
    public int RuleID { get; set; }

    [Required(ErrorMessage = "Referens är obligatorisk.")]
    public string Keyword { get; set; }  // Ex: "ICA", "SJ", "Spotify"

    [Required(ErrorMessage = "Kategori är obligatorisk.")]
    [ForeignKey("Category")]
    public int CategoryID { get; set; }

    public Category Category { get; set; }
}
