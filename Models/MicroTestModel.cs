using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.AccessControl;
using System.Threading.Tasks;

namespace SRIMAK.Models
{
    public class MicroTestModel
    {
        public string Code { get; set; }
        public string Date { get; set; }
        public int Result { get; set; }

        [Display(Name = "Coliform Cnt. Final")] public double colFinal { get; set; }
        [Display(Name = "Coliform Cnt. Well")] public double colWell { get; set; }
        [Display(Name = "E. coli Cnt.")] public double ecoli { get; set; }

    }
}
