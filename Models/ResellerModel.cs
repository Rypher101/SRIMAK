using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Permissions;
using System.Threading.Tasks;

namespace SRIMAK.Models
{
    public class ResellerModel
    {
        [Display(Name = "User ID")] public string userID { get; set; }
        [Required] public string Name { get; set; }
        [Display(Name = "Date of Appointment")] [Required] public string doa { get; set; }
        [Display(Name = "Contact Number")] [Required] public string contact { get; set; }
        [Required] public string Address { get; set; }
        [Required] public string Email { get; set; }
        [Required] public int Rout { get; set; }
        public string Town { get; set; }

    }
}
