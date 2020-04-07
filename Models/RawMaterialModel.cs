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

        [Display(Name = "Requested QTY")] public int Request { get; set; }

        [Display(Name="Last Requested Date")]
        public string ReqDate { get; set; }   

    }
}
