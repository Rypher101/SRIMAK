using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SRIMAK.Models
{
    public class RawMaterialModel
    {
        [Display(Name = "Material ID")] public int Id { get; set; }

        [Required] public string Name { get; set; }
        [Required] public int Size { get; set; }
        [Required] public int QTY { get; set; }
        [Required] public int ROL { get; set; }
        public int Buffer { get; set; }
        public int Consumption { get; set; }
        [Display(Name = "Stock Level")]public int Stock { get; set; }
        [Display(Name = "Req. QTY")] public int Request { get; set; }

        [Display(Name="Req. Date")]
        public string ReqDate { get; set; }   

    }
}
