using System;
using System.ComponentModel.DataAnnotations;

namespace SRIMAK.Models
{
    public class DailyProductionModel
    {
        public DateTime Date { get; set; }
        [Display(Name = "Mat ID")] public int rmID { get; set; }
        public string Name { get; set; }
        public int Production { get; set; }
        public int Wastage { get; set; }
        
    }
}