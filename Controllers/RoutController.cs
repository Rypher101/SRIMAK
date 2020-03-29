using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SRIMAK.Models;
using MySql.Data.MySqlClient;
using dto= SRIMAK.Models;

namespace SRIMAK.Controllers
{
    public class RoutController : Controller
    {
        private DBConnection DBConn { get; set; }

        public RoutController(DBConnection DB)
        {
            this.DBConn = DB;
        }

        private async Task<List<RoutModel>> GetRouts()
        {
            var ret = new List<RoutModel>();
            var cmd = this.DBConn.Connection.CreateCommand() as MySqlCommand;
            cmd.CommandText = @"SELECT * FROM rout";
            await using (var reader = await cmd.ExecuteReaderAsync()) while (await reader.ReadAsync())
            {
                var t = new RoutModel()
                {
                    RoutId = reader.GetFieldValue<int>(0),
                    Town = reader.GetFieldValue<string>(1),
                    User = reader.GetFieldValue<string>(2)
                };

                ret.Add(t);
            }

            return ret;
        }
        // GET: Rout
        public async Task<ActionResult> Index()
        {
            return View(await this.GetRouts());
        }

        // GET: Rout/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Rout/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Rout/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
               

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Rout/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Rout/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Rout/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Rout/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}