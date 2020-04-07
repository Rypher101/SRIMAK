using System.ComponentModel.DataAnnotations;

namespace SRIMAK.Models
{
    public class FinishedProductModel
    {
        [Display(Name = "Product ID")] public int pro_id { get; set; }

        [Required] public string Name { get; set; }
        [Required] public int QTY { get; set; }
        [Required] public decimal Price { get; set; }

        [Required] [Display(Name = "Material ID")] public int rm_id { get; set; }

        [Display(Name = "Material")] public string rm_name { get; set; }
    }
}