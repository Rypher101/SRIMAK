using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.AccessControl;
using System.Threading.Tasks;

namespace SRIMAK.Models
{
    public class SupplierModel
    {
        [Display(Name = "Supplier ID")] public int suppId { get; set; }
        [Display(Name = "User ID")] public string userId { get; set; }
        public string  Name { get; set; }
        [Display(Name = "Contact No")] public string Contact { get; set; }
        public string Email { get; set; }
        [Display(Name = "User Name")]public string uName { get; set; }
    }
}
