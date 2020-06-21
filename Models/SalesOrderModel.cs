using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SRIMAK.Models
{
    public class SalesOrderModel
    {
        [Display(Name = "Order ID")] public int soID { get; set; }
        [Display(Name = "Placed Date")] public string date { get; set; }
        [Display(Name = "Due Date")] public string dueDate { get; set; }
        public int disID { get; set; }
        [Display(Name = "Product ID")]public int proID { get; set; }
        [Display(Name = "Product")] public string prod { get; set; }
        public int QTY { get; set; }
        public decimal Cost { get; set; }
        public int Status { get; set; }
    }
}
