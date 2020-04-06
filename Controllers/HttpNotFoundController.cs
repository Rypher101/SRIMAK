using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace SRIMAK.Controllers
{
    public class HttpNotFoundController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}