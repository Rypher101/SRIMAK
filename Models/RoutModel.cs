using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SRIMAK.Models
{
    public class RoutModel
    {
        [Display(Name = "Rout ID")] public int RoutId { get; set; }
        
        public string Town { get; set; }
        
        public string User { get; set; }
        public string Name { get; set; }
    }
}
