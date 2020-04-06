using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
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
            //Session check
            if (CheckSession())
            {
                return RedirectToAction("Index", "Login", new{id = 1});
            }
            else
            {
                TempData["User"] = HttpContext.Session.GetString("Name");
            }

            var RawMaterial = await GetRawMaterials();

            ViewData["MatCount"] = RawMaterial.Count;
            SetActiveNavbar(1);

            return View(RawMaterial);
        }

        // GET Raw materials
        private async Task<List<RawMaterialModel>> GetRawMaterials(int x = 0, string pram1 = "rm_id", string pram2 = null)
        {
            try
            {
                var output = new List<RawMaterialModel>();
                var cmd = DBConn.Connection.CreateCommand();
                if (x == 1)
                {
                    cmd.CommandText = "SELECT * FROM raw_materials";
                }
                else if (x == 2)
                {
                    cmd.CommandText = "SELECT * FROM raw_materials ORDER BY " + pram1 + " " + pram2 ;
                }
                else
                {
                    cmd.CommandText = "SELECT * FROM raw_materials WHERE qty<=rol OR request>0";
                }
               
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
                            Id = reader.GetFieldValue<int>(0),
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

        public async Task<ActionResult> Materials(string sortPram = null, string typePram = null)
        {
            //Session check
            if (CheckSession())
            {
                return RedirectToAction("Index", "Login", new { id = 1 });
            }
            else
            {
                TempData["User"] = HttpContext.Session.GetString("Name");
            }

            Debug.WriteLine(sortPram);
            Debug.WriteLine(typePram);

            if (typePram == null || typePram == "ASC")
            {
                ViewData["sortType"] = "DESC";
            }else if (typePram == "DESC")
            {
                ViewData["sortType"] = "ASC";
            }
            ViewData["User"] = HttpContext.Session.GetString("Name");
            SetActiveNavbar(3);

            if (sortPram == null)
            {
                return View(await GetRawMaterials(1));
            }
            else
            {
                return View(await GetRawMaterials(2,sortPram,typePram));
            }
            
        }

        public async Task<ActionResult> EditMaterial(int id)
        {
            //Session check
            if (CheckSession())
            {
                return RedirectToAction("Index", "Login", new { id = 1 });
            }
            else
            {
                TempData["User"] = HttpContext.Session.GetString("Name");
            }

            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText = "SELECT * FROM raw_materials WHERE rm_id=@rmid";
                cmd.Parameters.AddWithValue("@rmid", id);

                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var temp = new RawMaterialModel
                        {
                            Id = reader.GetFieldValue<int>(0),
                            Name = reader.GetFieldValue<string>(1),
                            Size = reader.GetFieldValue<int>(2),
                            QTY = reader.GetFieldValue<int>(3),
                            ROL = reader.GetFieldValue<int>(4),
                        };

                        return View(temp);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return RedirectToAction("Index", "HttpNotFound");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditMaterialResult(IFormCollection collection)
        {
            //Session check
            if (CheckSession())
            {
                return RedirectToAction("Index", "Login", new { id = 1 });
            }
            else
            {
                TempData["User"] = HttpContext.Session.GetString("Name");
            }

            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText =
                    "UPDATE raw_materials SET name=@name, rm_size=@rmsize, qty=@qty, rol=@rol WHERE rm_id=@rmid";
                cmd.Parameters.AddWithValue("@name", collection["Name"]);
                cmd.Parameters.AddWithValue("@rmsize", collection["Size"]);
                cmd.Parameters.AddWithValue("@qty", collection["QTY"]);
                cmd.Parameters.AddWithValue("@rol", collection["ROL"]);
                cmd.Parameters.AddWithValue("@rmid", collection["Id"]);

                var recs = cmd.ExecuteNonQuery();
                if (recs > 0)
                {
                    TempData["Message"] = "Update Successful! : Material ID = " + collection["Id"];
                    TempData["MsgType"] = "2";
                }
                else
                {
                    TempData["Message"] = "Update Faild! : Material ID = " + collection["Id"];
                    TempData["MsgType"] = "4";
                }

                return RedirectToAction(nameof(Materials));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                TempData["Message"] = "Update Faild! : Material ID = " + collection["Id"];
                TempData["MsgType"] = "4";
                return RedirectToAction(nameof(Materials));
            }
        }

        public ActionResult DeleteMaterialResult(int id)
        {
            //Session check
            if (CheckSession())
            {
                return RedirectToAction("Index", "Login", new { id = 1 });
            }
            else
            {
                TempData["User"] = HttpContext.Session.GetString("Name");
            }

            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText = "DELETE FROM raw_materials WHERE rm_id = @rmid";
                cmd.Parameters.AddWithValue("@rmid", id);

                var recs = cmd.ExecuteNonQuery();

                if (recs > 0)
                {
                    TempData["Message"] = "Delete Successfull : Material ID = " + id ;
                    TempData["MsgType"] = "2";
                }
                else
                {
                    TempData["Message"] = "Delete Faild : Material ID = " + id ;
                    TempData["MsgType"] = "4";
                }

                return RedirectToAction(nameof(Materials));
            }
            catch (Exception e)
            {
                TempData["Message"] = "Delete Faild : Material ID = " + id ;
                TempData["MsgType"] = "4";
                Console.WriteLine(e);
                return RedirectToAction(nameof(Materials));
            }
        }

        public ActionResult CreateNewMaterial()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateNewMaterialResult(IFormCollection collection)
        {
            //Session check
            if (CheckSession())
            {
                return RedirectToAction("Index", "Login", new { id = 1 });
            }
            else
            {
                TempData["User"] = HttpContext.Session.GetString("Name");
            }

            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText = "INSERT INTO raw_materials (name,rm_size,qty,rol) VALUES(@name,@size,@qty,@rol)";
                cmd.Parameters.AddWithValue("@name", collection["Name"]);
                cmd.Parameters.AddWithValue("@size", collection["Size"]);
                cmd.Parameters.AddWithValue("@qty", collection["QTY"]);
                cmd.Parameters.AddWithValue("@rol", collection["ROL"]);

                var recs = cmd.ExecuteNonQuery();

                if (recs > 0)
                {
                    TempData["Message"] = "New Material Created!";
                    TempData["MsgType"] = "2";
                    return RedirectToAction(nameof(ShowNewMaterial));
                }
                else
                {
                    TempData["Message"] = "Error Occured while creating the new material. Please try again!";
                    TempData["MsgType"] = "4";
                    return RedirectToAction(nameof(CreateNewMaterial));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                TempData["Message"] = "Error Occured while creating the new material. Please try again!";
                TempData["MsgType"] = "4";
                return RedirectToAction(nameof(CreateNewMaterial));
            }

        }

        public async Task<ActionResult> ShowNewMaterial()
        {
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText = "SELECT MAX(rm_id),name,rm_size,qty,rol FROM raw_materials";

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new RawMaterialModel
                    {
                        Id = reader.GetFieldValue<int>(0),
                        Name = reader.GetFieldValue<string>(1),
                        Size = reader.GetFieldValue<int>(2),
                        QTY = reader.GetFieldValue<int>(3),
                        ROL = reader.GetFieldValue<int>(4),

                    };
                    TempData["Message"] = "New Material Created!";
                    TempData["MsgType"] = "2";
                    return View(temp);
                }
            }

            TempData["Message"] = "Unable to view new material!";
            TempData["MsgType"] = "4";
            return RedirectToAction(nameof(CreateNewMaterial));
        }
        private void SetActiveNavbar(int x)
        {
            switch (x)
            {
                case 1:
                    ViewData["Dashboard"] = "active";
                    ViewData["Distributor"] = "";
                    ViewData["Materials"] = "";
                    ViewData["Products"] = "";
                    ViewData["Resellers"] = "";
                    ViewData["Suppliers"] = "";
                    ViewData["Purchase"] = "";
                    break;

                case 2:
                    ViewData["Dashboard"] = "";
                    ViewData["Distributor"] = "active";
                    ViewData["Materials"] = "";
                    ViewData["Products"] = "";
                    ViewData["Resellers"] = "";
                    ViewData["Suppliers"] = "";
                    ViewData["Purchase"] = "";
                    break;

                case 3:
                    ViewData["Dashboard"] = "";
                    ViewData["Distributor"] = "";
                    ViewData["Materials"] = "active";
                    ViewData["Products"] = "";
                    ViewData["Resellers"] = "";
                    ViewData["Suppliers"] = "";
                    ViewData["Purchase"] = "";
                    break;

                case 4:
                    ViewData["Dashboard"] = "";
                    ViewData["Distributor"] = "";
                    ViewData["Materials"] = "";
                    ViewData["Products"] = "active";
                    ViewData["Resellers"] = "";
                    ViewData["Suppliers"] = "";
                    ViewData["Purchase"] = "";
                    break;

                case 5:
                    ViewData["Dashboard"] = "";
                    ViewData["Distributor"] = "";
                    ViewData["Materials"] = "";
                    ViewData["Products"] = "";
                    ViewData["Resellers"] = "active";
                    ViewData["Suppliers"] = "";
                    ViewData["Purchase"] = "";
                    break;
                case 6:
                    ViewData["Dashboard"] = "";
                    ViewData["Distributor"] = "";
                    ViewData["Materials"] = "";
                    ViewData["Products"] = "";
                    ViewData["Resellers"] = "";
                    ViewData["Suppliers"] = "active";
                    ViewData["Purchase"] = "";
                    break;

                case 7:
                    ViewData["Dashboard"] = "";
                    ViewData["Distributor"] = "";
                    ViewData["Materials"] = "";
                    ViewData["Products"] = "";
                    ViewData["Resellers"] = "";
                    ViewData["Suppliers"] = "";
                    ViewData["Purchase"] = "active";
                    break;
            }

        }

        private bool CheckSession()
        {
            if (HttpContext.Session.GetString("Name")!=null)
            {
                return false;
            }
            else
            {
                return false;
            }
        }

       
    }
}