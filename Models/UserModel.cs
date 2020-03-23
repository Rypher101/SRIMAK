using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MySql.Data.Types;

namespace SRIMAK.Models
{
    public class UserModel
    {
        public string UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime DOA { get; set; }
        public string Contact { get; set; }
        public string Address { get; set; }
        public int Type { get; set; }
    }
}
