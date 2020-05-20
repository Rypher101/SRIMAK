using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SRIMAK.Models
{
    public class PurchaseOrderModel
    {
        [Display(Name="ID")]public int poid { get; set; }
        [Display(Name="Supplier ID")]public int sup { get; set; }
        [Display(Name = "Supplier")] public string name { get; set; }
        [Display(Name = "Date")]public string Date { get; set; }
        
        public decimal Cost { get; set; }
        public string EDD { get; set; }
        public string Status { get; set; }
    }
}
