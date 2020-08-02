using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using MimeKit;

namespace SRIMAK.Models
{
    public class DailyIndoorTestModel
    {
        public string Code { get; set; }
        public string Date { get; set; }
        public int Result { get; set; }
        public double PH { get; set; }
        public double Hardness { get; set; }
        [Display(Name = "Fe. Composition")] public double fe { get; set; }
        [Display(Name ="Opt. PH Level")] public double optPH { get; set; }
        [Display(Name = "Opt. PH Composition")] public double optPHCo { get; set; }
    }
}
