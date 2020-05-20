using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SRIMAK.Models
{
    public class PurchaseOrderMaterialsModel
    {
        public int ID { get; set; }
        public string Name { get; set; }
        [Display(Name = "Per Unit")] public decimal perCost { get; set; }
        public int QTY { get; set; }
        public decimal Cost { get; set; }

    }
}
