using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace SRIMAK.Models
{
    public class LoginModel
    {
        [Display(Name = "User Name")]
        [Required]
        public string UserName { get; set; }

        [Required]
        [ProtectedPersonalData]
        public string Password { get; set; }

        
    }

}
