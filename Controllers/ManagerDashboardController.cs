using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SRIMAK.Models;

namespace SRIMAK.Controllers
{
    public class ManagerDashboardController : Controller
    {
        public ManagerDashboardController(DBConnection DB)
        {
            DBConn = DB;
        }

        private DBConnection DBConn { get; }

        // GET: ManagerDashboard
        public async Task<ActionResult> Index()
        {
            ViewData["User"] = HttpContext.Session.GetString("Name");

            var RawMaterial = await GetRawMaterials();

            ViewData["MatCount"] = RawMaterial.Count;

            return View(RawMaterial);
        }

        // GET Raw materials
        private async Task<List<RawMaterialModel>> GetRawMaterials()
        {
            try
            {
                var output = new List<RawMaterialModel>();
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText = "SELECT * FROM raw_materials WHERE qty<=rol OR request>0";
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var tempdate = "";

                        if (reader.GetDateTime(6) == DateTime.Parse("0001-1-1"))
                        {
                            tempdate = "-";
                        }
                        else
                        {
                            tempdate = reader.GetDateTime(6).ToString("D");
                        }
                        var temp = new RawMaterialModel
                        {
                            Id = reader.GetFieldValue<string>(0),
                            Name = reader.GetFieldValue<string>(1),
                            Size = reader.GetFieldValue<int>(2),
                            QTY = reader.GetFieldValue<int>(3),
                            ROL = reader.GetFieldValue<int>(4),
                            Request = reader.GetFieldValue<int>(5),
                            ReqDate = tempdate
                        };

                        output.Add(temp);
                    }
                }

                return output;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
           
        }

        // GET purchase orders
        private async Task<List<PurchaseOrderModel>> GetPurchaseOrder()
        {
            var output = new List<PurchaseOrderModel>();
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM purchase_order WHERE sup_id IS NULL";
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new PurchaseOrderModel
                    {
                        POID = reader.GetFieldValue<int>(0),
                        Date = reader.GetFieldValue<DateTime>(1)
                    };

                    output.Add(temp);
                }
            }

            return output;
        }

        // GET: ManagerDashboard/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: ManagerDashboard/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: ManagerDashboard/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ManagerDashboard/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: ManagerDashboard/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: ManagerDashboard/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: ManagerDashboard/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}