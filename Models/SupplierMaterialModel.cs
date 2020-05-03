using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.AccessControl;
using System.Threading;
using System.Threading.Tasks;

namespace SRIMAK.Models
{
    public class SupplierMaterialModel
    {
        [Display(Name ="Supplier ID")] public int supId { get; set; }
        [Display(Name = "Supplier Name")] public string supName { get; set; }
        [Display(Name="Material ID")] public int rawId { get; set; }
        [Display(Name="Material Name")] public string rawName { get; set; }
        public int PET { get; set; }
        public decimal Cost { get; set; }
        [Display(Name = "Lead Time")]public decimal lead { get; set; }

    }
}
