using System.ComponentModel.DataAnnotations;
public class Category
{
    [Key]
    public int CategoryID { get; set; }

    [Required]
    public string Name { get; set; }  // Ex: "Mat", "Transport", "Nöje"
}

