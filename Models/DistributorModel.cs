using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SRIMAK.Models
{
    public class DistributorModel
    {
        [Display(Name = "Distributor Name")] public int dis_id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string NIC { get; set; }
        public string Contact { get; set; }
        [Display(Name = "Vehicle No")] public string vehi_no { get; set; }
        [Display(Name = "Vehicle Type")] public string vehi_type { get; set; }
        public int Rout { get; set; }
        public string Town { get; set; }
    }
}
