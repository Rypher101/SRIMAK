using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
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

        // DASHBOARD
        public async Task<ActionResult> Index()
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            var RawMaterial = await GetRawMaterials();

            ViewData["MatCount"] = RawMaterial.Count;

            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText = "SELECT name FROM raw_materials WHERE status = 3";
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                var mat = "";
                while (await reader.ReadAsync())
                    if (mat == "")
                        mat = reader.GetFieldValue<string>(0);
                    else
                        mat = mat + ", " + reader.GetFieldValue<string>(0);

                if (mat != "")
                {
                    TempData["Message"] = mat + " have met the ROL. Please refer the dashboard.";
                    TempData["MsgType"] = "4";
                }
            }

            cmd.CommandText = "UPDATE raw_materials SET status = 1 WHERE status = 3";
            cmd.ExecuteNonQuery();

            var tempDate2 = new DateTime(2000, 01, 01);
            var todayDate = DateTime.Today;

            cmd.CommandText = "SELECT meta_key, value1 FROM meta WHERE meta_key = 'rol'";
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync()) tempDate2 = reader.GetFieldValue<DateTime>(1);
            }

            if (tempDate2.Year.ToString() != todayDate.Year.ToString() ||
                tempDate2.Month.ToString() != todayDate.Month.ToString())
            {
                var rawList = new List<FinishedProductModel>();
                cmd.CommandText =
                    "SELECT finished_product.rm_id, AVG(sales_product.qty) FROM (finished_product INNER JOIN sales_product ON finished_product.pro_id = sales_product.pro_id) INNER JOIN sales_order ON sales_order.so_id = sales_product.so_id WHERE sales_order.status = 3 AND (YEAR(sales_order.date) = YEAR(CURRENT_DATE - INTERVAL 1 MONTH) AND MONTH(sales_order.date) = MONTH(CURRENT_DATE - INTERVAL 1 MONTH)) GROUP BY finished_product.rm_id";
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var temp = new FinishedProductModel
                        {
                            rm_id = reader.GetFieldValue<int>(0),
                            avgQTY = reader.GetFieldValue<decimal>(1)
                        };

                        rawList.Add(temp);
                    }
                }

                var dict1 = new Dictionary<int, decimal>();
                var dict2 = new Dictionary<int, decimal>();
                cmd.CommandText =
                    "SELECT raw_materials.rm_id, MAX(lead_time), buffer_stock FROM material_supplier INNER JOIN raw_materials ON material_supplier.rm_id = raw_materials.rm_id GROUP BY raw_materials.rm_id";
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        dict1.Add(reader.GetFieldValue<int>(0), reader.GetFieldValue<decimal>(1));
                        dict2.Add(reader.GetFieldValue<int>(0), reader.GetFieldValue<int>(2));
                    }
                }

                foreach (var item in rawList)
                {
                    cmd.Parameters.Clear();
                    var temp = item.avgQTY * dict1[item.rm_id] + dict2[item.rm_id];
                    cmd.CommandText = "UPDATE raw_materials SET rol=@rol WHERE rm_id = @rmid";
                    cmd.Parameters.AddWithValue("@rol", temp);
                    cmd.Parameters.AddWithValue("@rmid", item.rm_id);
                    cmd.ExecuteNonQuery();
                }
            }

            cmd.CommandText = "SELECT SUM(prod), SUM(wast) FROM daily_production";
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    ViewBag.Prod = Math.Round(reader.GetFieldValue<decimal>(0) / (reader.GetFieldValue<decimal>(0)+ reader.GetFieldValue<decimal>(1)) * 100, 2);
                    ViewBag.Wast = Math.Round(reader.GetFieldValue<decimal>(1) / (reader.GetFieldValue<decimal>(0) + reader.GetFieldValue<decimal>(1)) * 100, 2);
                }
            }

            SetActiveNavbar(1);
            ViewBag.temp = GetTemp();
            return View(RawMaterial);
        }

        //MATERIAL
        public async Task<ActionResult> Materials(string sortPram = null, string typePram = null)
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");


            if (typePram == null || typePram == "ASC")
                ViewData["sortType"] = "DESC";
            else if (typePram == "DESC") ViewData["sortType"] = "ASC";

            SetActiveNavbar(3);

            if (sortPram == null)
                return View(await GetRawMaterials(1));
            return View(await GetRawMaterials(2, sortPram, typePram));
        }

        public async Task<ActionResult> EditMaterial(int id)
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText = "SELECT * FROM raw_materials WHERE rm_id=@rmid";
                cmd.Parameters.AddWithValue("@rmid", id);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var temp = new RawMaterialModel
                    {
                        Id = reader.GetFieldValue<int>(0),
                        Name = reader.GetFieldValue<string>(1),
                        Size = reader.GetFieldValue<int>(2),
                        QTY = reader.GetFieldValue<int>(3),
                        ROL = reader.GetFieldValue<int>(4),
                        Buffer = reader.GetFieldValue<int>(5),
                        Consumption = reader.GetFieldValue<int>(6),
                        Stock = reader.GetFieldValue<int>(7)
                    };

                    return View(temp);
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
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText =
                    "UPDATE raw_materials SET name=@name, rm_size=@rmsize, qty=@qty, rol=@rol, buffer_stock=@buffer, max_consumption=@cons, stock_level=@stock WHERE rm_id=@rmid";
                cmd.Parameters.AddWithValue("@name", collection["Name"]);
                cmd.Parameters.AddWithValue("@rmsize", collection["Size"]);
                cmd.Parameters.AddWithValue("@qty", collection["QTY"]);
                cmd.Parameters.AddWithValue("@rol", collection["ROL"]);
                cmd.Parameters.AddWithValue("@buffer", collection["Buffer"]);
                cmd.Parameters.AddWithValue("@cons", collection["Consumption"]);
                cmd.Parameters.AddWithValue("@stock", collection["Stock"]);
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
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText = "UPDATE raw_materials SET status = 0 WHERE rm_id = @rmid";
                cmd.Parameters.AddWithValue("@rmid", id);

                var recs = cmd.ExecuteNonQuery();

                cmd.CommandText = "DELETE FROM material_supplier WHERE rm_id = @rmid";
                cmd.Parameters.AddWithValue("@rmid", id);
                cmd.ExecuteNonQuery();

                if (recs > 0)
                {
                    TempData["Message"] = "Delete Successfull : Material ID = " + id;
                    TempData["MsgType"] = "2";
                }
                else
                {
                    TempData["Message"] = "Delete Faild : Material ID = " + id;
                    TempData["MsgType"] = "4";
                }

                return RedirectToAction(nameof(Materials));
            }
            catch (Exception e)
            {
                TempData["Message"] = "Delete Faild : Material ID = " + id;
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
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText =
                    "INSERT INTO raw_materials (name,rm_size,qty,rol,buffer_stock,max_consumption,stock_level) VALUES(@name,@size,@qty,@rol,@buffer,@cons,@stock)";
                cmd.Parameters.AddWithValue("@name", collection["Name"]);
                cmd.Parameters.AddWithValue("@size", collection["Size"]);
                cmd.Parameters.AddWithValue("@qty", collection["QTY"]);
                cmd.Parameters.AddWithValue("@rol", collection["ROL"]);
                cmd.Parameters.AddWithValue("@buffer", collection["Buffer"]);
                cmd.Parameters.AddWithValue("@cons", collection["Consumption"]);
                cmd.Parameters.AddWithValue("@stock", collection["Stock"]);

                var recs = cmd.ExecuteNonQuery();

                if (recs > 0)
                {
                    TempData["Message"] = "New Material Created!";
                    TempData["MsgType"] = "2";
                    return RedirectToAction(nameof(ViewNewMaterial));
                }

                TempData["Message"] = "Error Occured while creating the new material. Please try again!";
                TempData["MsgType"] = "4";
                return RedirectToAction(nameof(CreateNewMaterial));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                TempData["Message"] = "Error Occured while creating the new material. Please try again!";
                TempData["MsgType"] = "4";
                return RedirectToAction(nameof(CreateNewMaterial));
            }
        }

        public async Task<ActionResult> ViewNewMaterial()
        {
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT MAX(rm_id),name,rm_size,qty,rol FROM raw_materials WHERE rm_id = (SELECT MAX(rm_id) FROM raw_materials)";

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
                        ROL = reader.GetFieldValue<int>(4)
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

        //FINISHED PRODUCT
        public async Task<ActionResult> FinishedProducts(string sortPram = null, string typePram = null)
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            var output = new List<FinishedProductModel>();
            var cmd = DBConn.Connection.CreateCommand();

            if (sortPram == null)
                cmd.CommandText =
                    "SELECT pro_id,finished_Product.name,finished_product.qty,price,finished_product.rm_id,raw_materials.name FROM finished_product INNER JOIN raw_materials ON finished_product.rm_id = raw_materials.rm_id WHERE finished_product.status = 1";
            else
                cmd.CommandText =
                    "SELECT pro_id,finished_Product.name,finished_product.qty,price,finished_product.rm_id,raw_materials.name FROM finished_product INNER JOIN raw_materials ON finished_product.rm_id = raw_materials.rm_id WHERE finished_product.status = 1 ORDER BY " +
                    sortPram + " " + typePram;

            if (typePram == null || typePram == "DESC")
                ViewData["sortType"] = "ASC";
            else
                ViewData["sortType"] = "DESC";

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new FinishedProductModel
                    {
                        pro_id = reader.GetFieldValue<int>(0),
                        Name = reader.GetFieldValue<string>(1),
                        QTY = reader.GetFieldValue<int>(2),
                        Price = reader.GetFieldValue<decimal>(3),
                        rm_id = reader.GetFieldValue<int>(4),
                        rm_name = reader.GetFieldValue<string>(5)
                    };

                    output.Add(temp);
                }
            }

            SetActiveNavbar(4);
            return View(output);
        }

        public async Task<ActionResult> EditProduct(int id)
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            var raw = await GetRawMaterials(1);
            var temp = new FinishedProductModel();

            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT pro_id,finished_Product.name,finished_product.qty,price,finished_product.rm_id,raw_materials.name FROM finished_product INNER JOIN raw_materials ON finished_product.rm_id = raw_materials.rm_id WHERE pro_id = @proid";
            cmd.Parameters.AddWithValue("@proid", id);

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    temp.pro_id = reader.GetFieldValue<int>(0);
                    temp.Name = reader.GetFieldValue<string>(1);
                    temp.QTY = reader.GetFieldValue<int>(2);
                    temp.Price = reader.GetFieldValue<decimal>(3);
                    temp.rm_id = reader.GetFieldValue<int>(4);
                    temp.rm_name = reader.GetFieldValue<string>(5);
                }
            }

            ViewBag.Raw = raw;
            return View(temp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditProductResult(IFormCollection collection)
        {
            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText =
                    "UPDATE finished_product SET name=@name, qty=@qty, price=@price, rm_id=@rmid WHERE pro_id = @proid";
                cmd.Parameters.AddWithValue("@name", collection["Name"]);
                cmd.Parameters.AddWithValue("@qty", collection["QTY"]);
                cmd.Parameters.AddWithValue("@price", collection["Price"]);
                cmd.Parameters.AddWithValue("@rmid", collection["rm_id"]);
                cmd.Parameters.AddWithValue("@proid", collection["pro_id"]);

                var recs = cmd.ExecuteNonQuery();

                if (recs > 0)
                {
                    TempData["Message"] = "Product update successful! : Product ID = " + collection["pro_id"];
                    TempData["MsgType"] = "2";
                }
                else
                {
                    TempData["Message"] = "Product update faild!";
                    TempData["MsgType"] = "4";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }


            return RedirectToAction(nameof(FinishedProducts));
        }

        public ActionResult DeleteProductResult(int id)
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText = "UPDATE finished_product SET status = 0 WHERE pro_id = @proid";
                cmd.Parameters.AddWithValue("@proid", id);

                var recs = cmd.ExecuteNonQuery();

                if (recs > 0)
                {
                    TempData["Message"] = "Delete Successfull : Product ID = " + id;
                    TempData["MsgType"] = "2";
                }
                else
                {
                    TempData["Message"] = "Delete Faild : Material ID = " + id;
                    TempData["MsgType"] = "4";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return RedirectToAction(nameof(FinishedProducts));
        }

        public async Task<ActionResult> CreateNewProduct()
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            var raw = await GetRawMaterials();
            if (raw.Count == 0)
            {
                TempData["Message"] =
                    "There are no raw materials found. Please add raw materials before adding products.";
                TempData["MsgType"] = "4";
                return RedirectToAction(nameof(Materials));
            }

            ViewBag.Raw = raw;
            return View();
        }

        public ActionResult CreateNewProductResult(IFormCollection collection)
        {
            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText = "INSERT INTO finished_product(name,qty,price,rm_id) VALUES (@name,@qty,@price,@rmid)";
                cmd.Parameters.AddWithValue("@name", collection["Name"]);
                cmd.Parameters.AddWithValue("@qty", collection["QTY"]);
                cmd.Parameters.AddWithValue("@price", collection["Price"]);
                cmd.Parameters.AddWithValue("@rmid", collection["rm_id"]);

                var recs = cmd.ExecuteNonQuery();

                if (recs > 0)
                {
                    TempData["Message"] = "New product created!";
                    TempData["MsgType"] = "2";
                    return RedirectToAction(nameof(ViewNewProduct));
                }

                TempData["Message"] = "Error Occured while creating the new product. Please try again!";
                TempData["MsgType"] = "4";
            }
            catch (Exception e)
            {
                TempData["Message"] = "Error Occured while creating the new product. Please try again!";
                TempData["MsgType"] = "4";

                Console.WriteLine(e);
            }

            return RedirectToAction(nameof(FinishedProducts));
        }

        public async Task<ActionResult> ViewNewProduct()
        {
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT pro_id,finished_Product.name,finished_product.qty,price,finished_product.rm_id,raw_materials.name FROM finished_product INNER JOIN raw_materials ON finished_product.rm_id = raw_materials.rm_id WHERE pro_id = (SELECT MAX(pro_id) FROM finished_product)";

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new FinishedProductModel
                    {
                        pro_id = reader.GetFieldValue<int>(0),
                        Name = reader.GetFieldValue<string>(1),
                        QTY = reader.GetFieldValue<int>(2),
                        Price = reader.GetFieldValue<decimal>(3),
                        rm_id = reader.GetFieldValue<int>(4),
                        rm_name = reader.GetFieldValue<string>(5)
                    };

                    TempData["Message"] = "New product created!";
                    TempData["MsgType"] = "2";
                    return View(temp);
                }
            }

            TempData["Message"] = "Unable to view new product!";
            TempData["MsgType"] = "4";
            return RedirectToAction(nameof(FinishedProducts));
        }


        //Reseller
        public async Task<ActionResult> Resellers(string sortPram = null, string typePram = null)
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            var output = new List<ResellerModel>();
            var cmd = DBConn.Connection.CreateCommand();

            if (sortPram == null)
                cmd.CommandText =
                    "SELECT user.user_id,name,doa,contact_number,address,email,rout,town FROM user INNER JOIN rout ON user.rout = rout.rout_no WHERE type = 3";
            else
                cmd.CommandText =
                    "SELECT user.user_id,name,doa,contact_number,address,email,rout,town FROM user INNER JOIN rout ON user.rout = rout.rout_no WHERE type = 3 ORDER BY " +
                    sortPram + " " + typePram;

            if (typePram == null || typePram == "DESC")
                ViewData["sortType"] = "ASC";
            else
                ViewData["sortType"] = "DESC";

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new ResellerModel
                    {
                        userID = reader.GetFieldValue<string>(0),
                        Name = reader.GetFieldValue<string>(1),
                        doa = reader.GetFieldValue<DateTime>(2).ToString("D"),
                        contact = reader.GetFieldValue<string>(3),
                        Address = reader.GetFieldValue<string>(4),
                        Email = reader.GetFieldValue<string>(5),
                        Rout = reader.GetFieldValue<int>(6),
                        Town = reader.GetFieldValue<string>(7)
                    };

                    output.Add(temp);
                }
            }

            SetActiveNavbar(5);
            return View(output);
        }

        public async Task<ActionResult> CreateNewReseller()
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            var rout = await GetRouts();
            if (rout.Count == 0)
            {
                TempData["Message"] =
                    "There are no routs found. Please add routs before adding resellers.";
                TempData["MsgType"] = "4";
                return RedirectToAction(nameof(Resellers));
            }

            ViewBag.Raw = rout;
            return View();
        }

        public async Task<List<RoutModel>> GetRouts()
        {
            var output = new List<RoutModel>();
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT rout_no,town, rout.user_id,name FROM rout INNER JOIN user ON rout.user_id = user.user_id";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var temp = new RoutModel
                {
                    RoutId = reader.GetFieldValue<int>(0),
                    Town = reader.GetFieldValue<string>(1),
                    User = reader.GetFieldValue<string>(2),
                    Name = reader.GetFieldValue<string>(3)
                };

                output.Add(temp);
            }

            return output;
        }

        public ActionResult CreateNewResellerResult(IFormCollection collection)
        {
            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText =
                    "INSERT INTO user(user_id,password,name,doa,contact_number,address,type,email,rout) VALUES (@userid,@pass,@name,@doa,@contact,@address,3,@email,@rout)";
                //password: admin
                cmd.Parameters.AddWithValue("@pass",
                    "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918");
                cmd.Parameters.AddWithValue("@userid", collection["userID"]);
                cmd.Parameters.AddWithValue("@name", collection["Name"]);
                cmd.Parameters.AddWithValue("@doa", collection["doa"]);
                cmd.Parameters.AddWithValue("@contact", collection["contact"]);
                cmd.Parameters.AddWithValue("@address", collection["Address"]);
                cmd.Parameters.AddWithValue("@email", collection["Email"]);
                cmd.Parameters.AddWithValue("@rout", collection["rout_no"]);

                var recs = cmd.ExecuteNonQuery();

                if (recs > 0)
                {
                    TempData["Message"] = "New reseller registered!";
                    TempData["MsgType"] = "2";
                    return RedirectToAction("ViewNewReseller", new {user = collection["userID"]});
                }

                TempData["Message"] = "Error Occured while registering the new reseller. Please try again!";
                TempData["MsgType"] = "4";
            }
            catch (Exception e)
            {
                TempData["Message"] = "Error Occured while registering the new reseller. Please try again!";
                TempData["MsgType"] = "4";

                Console.WriteLine(e);
            }

            return RedirectToAction(nameof(Resellers));
        }

        public async Task<ActionResult> ViewNewReseller(string user)
        {
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT user.user_id,name,doa,contact_number,address,email,rout,town FROM user INNER JOIN rout ON user.rout = rout.rout_no WHERE user.user_id = '" +
                user + "'";

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new ResellerModel
                    {
                        userID = reader.GetFieldValue<string>(0),
                        Name = reader.GetFieldValue<string>(1),
                        doa = reader.GetFieldValue<DateTime>(2).ToString("D"),
                        contact = reader.GetFieldValue<string>(3),
                        Address = reader.GetFieldValue<string>(4),
                        Email = reader.GetFieldValue<string>(5),
                        Rout = reader.GetFieldValue<int>(6),
                        Town = reader.GetFieldValue<string>(7)
                    };

                    TempData["Message"] = "New reseller registered!";
                    TempData["MsgType"] = "2";
                    return View(temp);
                }
            }

            TempData["Message"] = "Unable to view new product!";
            TempData["MsgType"] = "4";
            return RedirectToAction(nameof(Resellers));
        }

        //Clerk
        public async Task<ActionResult> Clerk(string sortPram = null, string typePram = null)
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            var output = new List<ClerkModel>();
            var cmd = DBConn.Connection.CreateCommand();

            if (sortPram == null)
                cmd.CommandText =
                    "SELECT user.user_id,name,doa,contact_number,address,email FROM user WHERE type = 2 AND status = 1";
            else
                cmd.CommandText =
                    "SELECT user.user_id,name,doa,contact_number,address,email FROM user WHERE type = 2 AND status = 1 ORDER BY " +
                    sortPram + " " + typePram;

            if (typePram == null || typePram == "DESC")
                ViewData["sortType"] = "ASC";
            else
                ViewData["sortType"] = "DESC";

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new ClerkModel
                    {
                        userID = reader.GetFieldValue<string>(0),
                        Name = reader.GetFieldValue<string>(1),
                        doa = reader.GetFieldValue<DateTime>(2).ToString("D"),
                        contact = reader.GetFieldValue<string>(3),
                        Address = reader.GetFieldValue<string>(4),
                        Email = reader.GetFieldValue<string>(5)
                    };

                    output.Add(temp);
                }
            }

            SetActiveNavbar(9);
            return View(output);
        }

        public ActionResult DeleteClerkResult(string user)
        {
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText = "UPDATE user SET status = 0 WHERE user_id = @user";
            cmd.Parameters.AddWithValue("@user", user);

            var recs = cmd.ExecuteNonQuery();

            if (recs > 0)
            {
                TempData["Message"] = "Clerk deleted!";
                TempData["MsgType"] = "2";
            }
            else
            {
                TempData["Message"] = "Error occured while deleting the clerk";
                TempData["MsgType"] = "4";
            }

            return RedirectToAction(nameof(Clerk));
        }

        public ActionResult CreateNewClerk()
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            return View();
        }

        public ActionResult CreateNewClerkResult(IFormCollection collection)
        {
            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText =
                    "INSERT INTO user(user_id,password,name,doa,contact_number,address,type,email) VALUES (@userid,'admin',@name,@doa,@contact,@address,2,@email)";
                cmd.Parameters.AddWithValue("@userid", collection["userID"]);
                cmd.Parameters.AddWithValue("@name", collection["Name"]);
                cmd.Parameters.AddWithValue("@doa", collection["doa"]);
                cmd.Parameters.AddWithValue("@contact", collection["contact"]);
                cmd.Parameters.AddWithValue("@address", collection["Address"]);
                cmd.Parameters.AddWithValue("@email", collection["Email"]);

                var recs = cmd.ExecuteNonQuery();

                if (recs > 0)
                {
                    TempData["Message"] = "New Clerk registered!";
                    TempData["MsgType"] = "2";
                    return RedirectToAction("ViewNewClerk", new {user = collection["userID"]});
                }

                TempData["Message"] = "Error Occured while registering the new Clerk. Please try again!";
                TempData["MsgType"] = "4";
            }
            catch (Exception e)
            {
                TempData["Message"] = "Error Occured while registering the new Clerk. Please try again!";
                TempData["MsgType"] = "4";

                Console.WriteLine(e);
            }

            return RedirectToAction(nameof(Clerk));
        }

        public async Task<ActionResult> ViewNewClerk(string user)
        {
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT user.user_id,name,doa,contact_number,address,email FROM user WHERE user.user_id = '" +
                user + "'";

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new ClerkModel
                    {
                        userID = reader.GetFieldValue<string>(0),
                        Name = reader.GetFieldValue<string>(1),
                        doa = reader.GetFieldValue<DateTime>(2).ToString("D"),
                        contact = reader.GetFieldValue<string>(3),
                        Address = reader.GetFieldValue<string>(4),
                        Email = reader.GetFieldValue<string>(5)
                    };

                    TempData["Message"] = "New Clerk registered!";
                    TempData["MsgType"] = "2";
                    return View(temp);
                }
            }

            TempData["Message"] = "Unable to view new product!";
            TempData["MsgType"] = "4";
            return RedirectToAction(nameof(Clerk));
        }

        //SUPPLIER
        public async Task<ActionResult> Supplier(string sortPram = null, string typePram = null)
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            var output = new List<SupplierModel>();
            var cmd = DBConn.Connection.CreateCommand();

            if (sortPram == null)
                cmd.CommandText =
                    "SELECT sup_id,supplier.name,supplier.contact,supplier.email,supplier.user_id,user.name FROM supplier INNER JOIN user ON supplier.user_id = user.user_id WHERE supplier.status = 1";
            else
                cmd.CommandText =
                    "SELECT sup_id,supplier.name,supplier.contact,supplier.email,supplier.user_id,user.name FROM supplier INNER JOIN user ON supplier.user_id = user.user_id WHERE supplier.status = 1 ORDER BY " +
                    sortPram + " " + typePram;

            if (typePram == null || typePram == "DESC")
                ViewData["sortType"] = "ASC";
            else
                ViewData["sortType"] = "DESC";

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new SupplierModel
                    {
                        suppId = reader.GetFieldValue<int>(0),
                        Name = reader.GetFieldValue<string>(1),
                        Contact = reader.GetFieldValue<string>(2),
                        Email = reader.GetFieldValue<string>(3),
                        userId = reader.GetFieldValue<string>(4),
                        uName = reader.GetFieldValue<string>(5)
                    };

                    output.Add(temp);
                }
            }

            SetActiveNavbar(6);
            return View(output);
        }

        public async Task<ActionResult> EditSupplier(int id)
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            var temp = new SupplierModel();
            ;
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT * FROM supplier WHERE sup_id = @supid";
            cmd.Parameters.AddWithValue("@supid", id);

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    temp.suppId = reader.GetFieldValue<int>(0);
                    temp.Name = reader.GetFieldValue<string>(1);
                    temp.Contact = reader.GetFieldValue<string>(2);
                    temp.Email = reader.GetFieldValue<string>(3);
                }
            }

            return View(temp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditSupplierResult(IFormCollection collection)
        {
            if (HttpContext.Session.GetString("UID") == null)
            {
                TempData["Message"] = "Invalid session data. Please login and try again!";
                TempData["MsgType"] = "4";
                return RedirectToAction(nameof(Supplier));
            }

            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText =
                    "UPDATE supplier SET name=@name, contact=@contact, email=@email, user_id=@uid WHERE sup_id = @supid";
                cmd.Parameters.AddWithValue("@name", collection["Name"]);
                cmd.Parameters.AddWithValue("@contact", collection["Contact"]);
                cmd.Parameters.AddWithValue("@email", collection["Email"]);
                cmd.Parameters.AddWithValue("@uid", HttpContext.Session.GetString("UID"));
                cmd.Parameters.AddWithValue("@supid", collection["suppId"]);

                var recs = cmd.ExecuteNonQuery();

                if (recs > 0)
                {
                    TempData["Message"] = "Supplier update successful! : Supplier ID = " + collection["suppId"];
                    TempData["MsgType"] = "2";
                }
                else
                {
                    TempData["Message"] = "Supplier update faild!";
                    TempData["MsgType"] = "4";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }


            return RedirectToAction(nameof(Supplier));
        }

        public ActionResult DeleteSupplierResult(int id)
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText = "UPDATE supplier SET status = 0, user_id = @user WHERE sup_id = @supid";
                cmd.Parameters.AddWithValue("@user", HttpContext.Session.GetString("UID"));
                cmd.Parameters.AddWithValue("@supid", id);

                var recs = cmd.ExecuteNonQuery();

                if (recs > 0)
                {
                    TempData["Message"] = "Delete Successfull : Supplier ID = " + id;
                    TempData["MsgType"] = "2";
                }
                else
                {
                    TempData["Message"] = "Delete Faild : Supplier ID = " + id;
                    TempData["MsgType"] = "4";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return RedirectToAction(nameof(Supplier));
        }

        public ActionResult CreateNewSupplier()
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            if (HttpContext.Session.GetString("UID") == null)
            {
                TempData["Message"] = "Invalid session data. Please login and try again!";
                TempData["MsgType"] = "4";
                return RedirectToAction(nameof(Supplier));
            }

            return View();
        }

        public ActionResult CreateNewSupplierResult(IFormCollection collection)
        {
            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText =
                    "INSERT INTO supplier(name,contact,email,user_id) VALUES (@name,@contact,@email,@user)";
                cmd.Parameters.AddWithValue("@name", collection["Name"]);
                cmd.Parameters.AddWithValue("@contact", collection["Contact"]);
                cmd.Parameters.AddWithValue("@email", collection["Email"]);
                cmd.Parameters.AddWithValue("@user", HttpContext.Session.GetString("UID"));

                var recs = cmd.ExecuteNonQuery();

                if (recs > 0)
                {
                    TempData["Message"] = "New Supplier registered!";
                    TempData["MsgType"] = "2";
                    return RedirectToAction(nameof(ViewNewSupplier));
                }

                TempData["Message"] = "Error Occured while registering the new supplier. Please try again!";
                TempData["MsgType"] = "4";
            }
            catch (Exception e)
            {
                TempData["Message"] = "Error Occured while registering the new supplier. Please try again!";
                TempData["MsgType"] = "4";

                Console.WriteLine(e);
            }

            return RedirectToAction(nameof(FinishedProducts));
        }

        public async Task<ActionResult> ViewNewSupplier()
        {
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT sup_id,supplier.name,supplier.contact,supplier.email,supplier.user_id,user.name FROM supplier INNER JOIN user ON supplier.user_id = user.user_id WHERE supplier.status = 1 AND sup_id = (SELECT MAX(sup_id) FROM supplier)";

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new SupplierModel
                    {
                        suppId = reader.GetFieldValue<int>(0),
                        Name = reader.GetFieldValue<string>(1),
                        Contact = reader.GetFieldValue<string>(2),
                        Email = reader.GetFieldValue<string>(3),
                        userId = reader.GetFieldValue<string>(4),
                        uName = reader.GetFieldValue<string>(5)
                    };

                    TempData["Message"] = "New supplier created!";
                    TempData["MsgType"] = "2";
                    return View(temp);
                }
            }

            TempData["Message"] = "Unable to view new supplier!";
            TempData["MsgType"] = "4";
            return RedirectToAction(nameof(Supplier));
        }

        //SUPPLIER-MATERIAL
        public async Task<ActionResult> SupplierMaterial(int id, string sortPram = null, string typePram = null)
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            var output = new List<SupplierMaterialModel>();
            var cmd = DBConn.Connection.CreateCommand();
            var name = "";

            if (sortPram == null)
                cmd.CommandText =
                    "SELECT material_supplier.sup_id, supplier.name, material_supplier.rm_id, raw_materials.name, material_supplier.pet, material_supplier.cost, material_supplier.lead_time FROM (material_supplier INNER JOIN supplier ON material_supplier.sup_id = supplier.sup_id) INNER JOIN raw_materials ON material_supplier.rm_id = raw_materials.rm_id WHERE material_supplier.sup_id = @supid";
            else
                cmd.CommandText =
                    "SELECT material_supplier.sup_id, supplier.name, material_supplier.rm_id, raw_materials.Name, pet,cost,lead_time FROM material_supplier INNER JOIN supplier ON material_supplier.sup_id = supplier.sup_id INNER JOIN raw_materials ON material_supplier.rm_id = raw_materials.rm_id WHERE material_supplier.sup_id = @supid ORDER BY " +
                    sortPram + " " + typePram;

            cmd.Parameters.AddWithValue("@supid", id);

            if (typePram == null || typePram == "DESC")
                ViewData["sortType"] = "ASC";
            else
                ViewData["sortType"] = "DESC";

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new SupplierMaterialModel
                    {
                        supId = reader.GetFieldValue<int>(0),
                        supName = reader.GetFieldValue<string>(1),
                        rawId = reader.GetFieldValue<int>(2),
                        rawName = reader.GetFieldValue<string>(3),
                        PET = reader.GetFieldValue<int>(4),
                        Cost = reader.GetFieldValue<decimal>(5),
                        lead = reader.GetFieldValue<decimal>(6)
                    };

                    name = reader.GetFieldValue<string>(1);
                    output.Add(temp);
                }
            }

            ViewBag.ID = id;
            return View(output);
        }

        public async Task<ActionResult> EditSupplierMaterial(int supid, int rawid)
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            var temp = new SupplierMaterialModel();
            ;
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT material_supplier.sup_id, supplier.name, material_supplier.rm_id, raw_materials.name, material_supplier.pet, material_supplier.cost, material_supplier.lead_time FROM (material_supplier INNER JOIN supplier ON material_supplier.sup_id = supplier.sup_id) INNER JOIN raw_materials ON material_supplier.rm_id = raw_materials.rm_id WHERE material_supplier.sup_id = @supid AND material_Supplier.rm_id = @rawid";
            cmd.Parameters.AddWithValue("@supid", supid);
            cmd.Parameters.AddWithValue("@rawid", rawid);

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    temp.supId = reader.GetFieldValue<int>(0);
                    temp.supName = reader.GetFieldValue<string>(1);
                    temp.rawId = reader.GetFieldValue<int>(2);
                    temp.rawName = reader.GetFieldValue<string>(3);
                    temp.PET = reader.GetFieldValue<int>(4);
                    temp.Cost = reader.GetFieldValue<decimal>(5);
                    temp.lead = reader.GetFieldValue<decimal>(6);
                }
            }

            return View(temp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditSupplierMaterialResult(IFormCollection collection)
        {
            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText =
                    "UPDATE material_supplier SET pet=@pet, cost=@cost, lead_time=@lead WHERE sup_id = @supid AND rm_id = @rmid";
                cmd.Parameters.AddWithValue("@pet", collection["PET"]);
                cmd.Parameters.AddWithValue("@cost", collection["Cost"]);
                cmd.Parameters.AddWithValue("@lead", collection["lead"]);
                cmd.Parameters.AddWithValue("@supid", collection["supId"]);
                cmd.Parameters.AddWithValue("@rmid", collection["rawId"]);

                var recs = cmd.ExecuteNonQuery();

                if (recs > 0)
                {
                    TempData["Message"] = "Material update successful! : Material ID = " + collection["rawId"];
                    TempData["MsgType"] = "2";
                }
                else
                {
                    TempData["Message"] = "Material update faild!";
                    TempData["MsgType"] = "4";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }


            return RedirectToAction("SupplierMaterial", new {id = collection["supId"]});
        }

        public ActionResult DeleteSupplierMaterialResult(int supid, int rawid)
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText = "DELETE FROM material_supplier WHERE sup_id = @supid AND rm_id = @rawid";
                cmd.Parameters.AddWithValue("@supid", supid);
                cmd.Parameters.AddWithValue("@rawid", rawid);

                var recs = cmd.ExecuteNonQuery();

                if (recs > 0)
                {
                    TempData["Message"] = "Delete Successfull : Material ID = " + rawid;
                    TempData["MsgType"] = "2";
                }
                else
                {
                    TempData["Message"] = "Delete Faild : Material ID = " + rawid;
                    TempData["MsgType"] = "4";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return RedirectToAction("SupplierMaterial", new {id = supid});
        }

        public async Task<ActionResult> CreateNewSupplierMaterial(int supId)
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            var raw = await GetRawMaterials(3, supid: supId);
            if (raw.Count == 0)
            {
                TempData["Message"] =
                    "There are no raw materials found to allocate to this supplier";
                TempData["MsgType"] = "4";
                return RedirectToAction("SupplierMaterial", new {id = supId});
            }

            ViewBag.ID = supId;
            ViewBag.Raw = raw;
            return View();
        }

        public ActionResult CreateNewSupplierMaterialResult(IFormCollection collection)
        {
            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText =
                    "INSERT INTO material_supplier(sup_id,rm_id,pet,cost,material_supplier.lead_time) VALUES (@sup,@raw,@pet,@cost,@lead)";
                cmd.Parameters.AddWithValue("@sup", collection["supId"]);
                cmd.Parameters.AddWithValue("@raw", collection["rawId"]);
                cmd.Parameters.AddWithValue("@pet", collection["PET"]);
                cmd.Parameters.AddWithValue("@cost", collection["Cost"]);
                cmd.Parameters.AddWithValue("@lead", collection["Lead"]);

                var recs = cmd.ExecuteNonQuery();

                if (recs > 0)
                {
                    TempData["Message"] = "New material registered!";
                    TempData["MsgType"] = "2";
                }
                else
                {
                    TempData["Message"] = "Error Occured while registering the new supplier. Please try again!";
                    TempData["MsgType"] = "4";
                }
            }
            catch (Exception e)
            {
                TempData["Message"] = "Error Occured while registering the new supplier!";
                TempData["MsgType"] = "4";

                Console.WriteLine(e);
            }

            return RedirectToAction("SupplierMaterial", new {id = collection["supId"]});
        }

        //DISTRIBUTOR
        public async Task<ActionResult> Distributor(string sortPram = null, string typePram = null)
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            var output = new List<DistributorModel>();
            var cmd = DBConn.Connection.CreateCommand();

            if (sortPram == null)
                cmd.CommandText =
                    "SELECT dis_id, name, email, nic, contact, vehi_no, vehi_type, distributor.rout_no, rout.town FROM distributor INNER JOIN rout ON distributor.rout_no = rout.rout_no WHERE distributor.status = 1";
            else
                cmd.CommandText =
                    "SELECT dis_id, name, email, nic, contact, vehi_no, vehi_type, distributor.rout_no, rout.town FROM distributor INNER JOIN rout ON distributor.rout_no = rout.rout_no WHERE distributor.status = 1 ORDER BY " +
                    sortPram + " " + typePram;

            if (typePram == null || typePram == "DESC")
                ViewData["sortType"] = "ASC";
            else
                ViewData["sortType"] = "DESC";

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new DistributorModel
                    {
                        dis_id = reader.GetFieldValue<int>(0),
                        Name = reader.GetFieldValue<string>(1),
                        Email = reader.GetFieldValue<string>(2),
                        NIC = reader.GetFieldValue<string>(3),
                        Contact = reader.GetFieldValue<string>(4),
                        vehi_no = reader.GetFieldValue<string>(5),
                        vehi_type = reader.GetFieldValue<string>(6),
                        Rout = reader.GetFieldValue<int>(7),
                        Town = reader.GetFieldValue<string>(8)
                    };

                    output.Add(temp);
                }
            }

            SetActiveNavbar(2);
            return View(output);
        }

        public async Task<ActionResult> EditDistributor(int id)
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            var rout = await GetRouts();
            if (rout.Count == 0)
            {
                TempData["Message"] =
                    "There are no routs found. Please add routs before adding distributors.";
                TempData["MsgType"] = "4";
                return RedirectToAction(nameof(Distributor));
            }

            var temp = new DistributorModel();

            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT dis_id, name, email, nic, contact, vehi_no, vehi_type, distributor.rout_no, rout.town FROM distributor INNER JOIN rout ON distributor.rout_no = rout.rout_no WHERE distributor.status = 1 AND dis_id = @disid";
            cmd.Parameters.AddWithValue("@disid", id);

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    temp.dis_id = reader.GetFieldValue<int>(0);
                    temp.Name = reader.GetFieldValue<string>(1);
                    temp.Email = reader.GetFieldValue<string>(2);
                    temp.NIC = reader.GetFieldValue<string>(3);
                    temp.Contact = reader.GetFieldValue<string>(4);
                    temp.vehi_no = reader.GetFieldValue<string>(5);
                    temp.vehi_type = reader.GetFieldValue<string>(6);
                    temp.Rout = reader.GetFieldValue<int>(7);
                    temp.Town = reader.GetFieldValue<string>(8);
                }
            }

            ViewBag.Routs = rout;
            return View(temp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditDistributorResult(IFormCollection collection)
        {
            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText =
                    "UPDATE distributor SET name=@name , email=@email, nic=@nic, contact=@contact, vehi_no=@no, vehi_type=@type, rout_no=@rout WHERE dis_id = @disid";
                cmd.Parameters.AddWithValue("@name", collection["Name"]);
                cmd.Parameters.AddWithValue("@email", collection["Email"]);
                cmd.Parameters.AddWithValue("@nic", collection["NIC"]);
                cmd.Parameters.AddWithValue("@contact", collection["Contact"]);
                cmd.Parameters.AddWithValue("@no", collection["vehi_no"]);
                cmd.Parameters.AddWithValue("@type", collection["vehi_type"]);
                cmd.Parameters.AddWithValue("@rout", collection["Rout"]);
                cmd.Parameters.AddWithValue("@disid", collection["dis_id"]);

                var recs = cmd.ExecuteNonQuery();

                if (recs > 0)
                {
                    TempData["Message"] = "Distributor update successful! : Distributor ID = " + collection["dis_id"];
                    TempData["MsgType"] = "2";
                }
                else
                {
                    TempData["Message"] = "Distributor update faild!";
                    TempData["MsgType"] = "4";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }


            return RedirectToAction(nameof(Distributor));
        }

        public ActionResult DeleteDistributorResult(int id)
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText = "UPDATE distributor SET status = 0 WHERE dis_id = @disid";
                cmd.Parameters.AddWithValue("@disid", id);

                var recs = cmd.ExecuteNonQuery();

                if (recs > 0)
                {
                    TempData["Message"] = "Delete Successfull : Distributor ID = " + id;
                    TempData["MsgType"] = "2";
                }
                else
                {
                    TempData["Message"] = "Delete Faild : Distributor ID = " + id;
                    TempData["MsgType"] = "4";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return RedirectToAction(nameof(Distributor));
        }

        public async Task<ActionResult> CreateNewDistributor()
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            var rout = await GetRouts();
            if (rout.Count == 0)
            {
                TempData["Message"] =
                    "There are no routs found. Please add routs before adding distributors.";
                TempData["MsgType"] = "4";
                return RedirectToAction(nameof(Distributor));
            }

            ViewBag.Routs = rout;

            return View();
        }

        public ActionResult CreateNewDistributorResult(IFormCollection collection)
        {
            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText =
                    "INSERT INTO distributor(name,email,nic,contact,vehi_no,vehi_type,rout_no) VALUES (@name,@email,@nic,@contact,@no,@type,@rout)";
                cmd.Parameters.AddWithValue("@name", collection["Name"]);
                cmd.Parameters.AddWithValue("@email", collection["Email"]);
                cmd.Parameters.AddWithValue("@nic", collection["NIC"]);
                cmd.Parameters.AddWithValue("@contact", collection["Contact"]);
                cmd.Parameters.AddWithValue("@no", collection["vehi_no"]);
                cmd.Parameters.AddWithValue("@type", collection["vehi_type"]);
                cmd.Parameters.AddWithValue("@rout", collection["Rout"]);

                var recs = cmd.ExecuteNonQuery();

                if (recs > 0)
                {
                    TempData["Message"] = "New Distributor registered!";
                    TempData["MsgType"] = "2";
                    return RedirectToAction(nameof(ViewNewDistributor));
                }

                TempData["Message"] = "Error Occured while registering the new distributor. Please try again!";
                TempData["MsgType"] = "4";
            }
            catch (Exception e)
            {
                TempData["Message"] = "Error Occured while registering the new distributor. Please try again!";
                TempData["MsgType"] = "4";

                Console.WriteLine(e);
            }

            return RedirectToAction(nameof(Distributor));
        }

        public async Task<ActionResult> ViewNewDistributor()
        {
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT dis_id, name, email, nic, contact, vehi_no, vehi_type, distributor.rout_no, rout.town FROM distributor INNER JOIN rout ON distributor.rout_no = rout.rout_no WHERE status = 1 AND dis_id = (SELECT MAX(dis_id) FROM distributor)";

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new DistributorModel
                    {
                        dis_id = reader.GetFieldValue<int>(0),
                        Name = reader.GetFieldValue<string>(1),
                        Email = reader.GetFieldValue<string>(2),
                        NIC = reader.GetFieldValue<string>(3),
                        Contact = reader.GetFieldValue<string>(4),
                        vehi_no = reader.GetFieldValue<string>(5),
                        vehi_type = reader.GetFieldValue<string>(6),
                        Rout = reader.GetFieldValue<int>(7),
                        Town = reader.GetFieldValue<string>(8)
                    };

                    TempData["Message"] = "New distributor registered!";
                    TempData["MsgType"] = "2";
                    return View(temp);
                }
            }

            TempData["Message"] = "Unable to view new distributor!";
            TempData["MsgType"] = "4";
            return RedirectToAction(nameof(Distributor));
        }

        //ROUT
        public async Task<ActionResult> Rout(string sortPram = null, string typePram = null)
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            var output = new List<RoutModel>();
            var cmd = DBConn.Connection.CreateCommand();

            if (sortPram == null)
                cmd.CommandText =
                    "SELECT rout_no,town,rout.user_id,user.name FROM rout INNER JOIN user ON rout.user_id = user.user_id WHERE rout.status = 1";
            else
                cmd.CommandText =
                    "SELECT rout_no,town,rout.user_id,user.name FROM rout INNER JOIN user ON rout.user_id = user.user_id WHERE rout.status = 1 ORDER BY " +
                    sortPram + " " + typePram;

            if (typePram == null || typePram == "DESC")
                ViewData["sortType"] = "ASC";
            else
                ViewData["sortType"] = "DESC";

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new RoutModel
                    {
                        RoutId = reader.GetFieldValue<int>(0),
                        Town = reader.GetFieldValue<string>(1),
                        User = reader.GetFieldValue<string>(2),
                        Name = reader.GetFieldValue<string>(3)
                    };

                    output.Add(temp);
                }
            }

            SetActiveNavbar(8);
            return View(output);
        }

        public async Task<ActionResult> EditRout(int id)
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            if (HttpContext.Session.GetString("UID") == null)
            {
                TempData["Message"] = "Invalid session data. Please login and try again!";
                TempData["MsgType"] = "4";
                return RedirectToAction(nameof(Rout));
            }

            var temp = new RoutModel();

            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT rout_no,town FROM rout WHERE status = 1 AND rout_no = @routid";
            cmd.Parameters.AddWithValue("@routid", id);

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    temp.RoutId = reader.GetFieldValue<int>(0);
                    temp.Town = reader.GetFieldValue<string>(1);
                }
            }

            return View(temp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditRoutResult(IFormCollection collection)
        {
            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText = "SELECT rout_no FROM rout WHERE rout_no = @rout";
                cmd.Parameters.AddWithValue("@rout", collection["RoutId"]);
                var recs = cmd.ExecuteNonQuery();

                if (recs > 0)
                {
                    TempData["Message"] = "This rout number is already assigned for another rout";
                    TempData["MsgType"] = "4";
                    return RedirectToAction("EditRout", new {id = collection["Rout2"]});
                }

                cmd.CommandText =
                    "UPDATE rout SET rout_no=@rout3,town=@town,user_id=@user  WHERE rout_no = @rout2";
                cmd.Parameters.AddWithValue("@rout3", collection["RoutId"]);
                cmd.Parameters.AddWithValue("@town", collection["Town"]);
                cmd.Parameters.AddWithValue("@user", HttpContext.Session.GetString("UID"));
                cmd.Parameters.AddWithValue("@rout2", collection["Rout2"]);

                var recs2 = cmd.ExecuteNonQuery();

                if (recs2 > 0)
                {
                    TempData["Message"] = "Rout update successful! : Rout ID = " + collection["RoutId"];
                    TempData["MsgType"] = "2";
                }
                else
                {
                    TempData["Message"] = "Rout update faild!";
                    TempData["MsgType"] = "4";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }


            return RedirectToAction(nameof(Rout));
        }

        public ActionResult DeleteRoutResult(int id)
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText = "UPDATE rout SET status = 0 WHERE rout_no = @routid";
                cmd.Parameters.AddWithValue("@routid", id);

                var recs = cmd.ExecuteNonQuery();

                if (recs > 0)
                {
                    TempData["Message"] = "Delete Successfull : Rout ID = " + id;
                    TempData["MsgType"] = "2";
                }
                else
                {
                    TempData["Message"] = "Delete Faild : Rout ID = " + id;
                    TempData["MsgType"] = "4";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return RedirectToAction(nameof(Rout));
        }

        public async Task<ActionResult> CreateNewRout()
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            if (HttpContext.Session.GetString("UID") == null)
            {
                TempData["Message"] = "Invalid session data. Please login and try again!";
                TempData["MsgType"] = "4";
                return RedirectToAction(nameof(Rout));
            }

            var routNo = 0;
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText = "SELECT MAX(rout_no) FROM rout";
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync()) routNo = reader.GetFieldValue<int>(0);
            }

            ViewBag.RoutNo = routNo + 1;
            return View();
        }

        public ActionResult CreateNewRoutResult(IFormCollection collection)
        {
            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText = "SELECT rout_no FROM rout WHERE rout_no = @rout";
                cmd.Parameters.AddWithValue("@rout", collection["RoutId"]);
                var recs = cmd.ExecuteNonQuery();

                if (recs > 0)
                {
                    TempData["Message"] = "This rout number is already assigned for another rout";
                    TempData["MsgType"] = "4";
                    return RedirectToAction(nameof(CreateNewRout));
                }

                cmd.CommandText =
                    "INSERT INTO rout(rout_no,town,user_id) VALUES (@rout2,@town,@user)";
                cmd.Parameters.AddWithValue("@rout2", collection["RoutId"]);
                cmd.Parameters.AddWithValue("@town", collection["Town"]);
                cmd.Parameters.AddWithValue("@user", HttpContext.Session.GetString("UID"));
                var recs2 = cmd.ExecuteNonQuery();

                if (recs2 > 0)
                {
                    TempData["Message"] = "New Rout registered!";
                    TempData["MsgType"] = "2";
                    return RedirectToAction("ViewNewRout", new {rout = collection["RoutId"]});
                }

                TempData["Message"] = "Error Occured while registering the new rout. Please try again!";
                TempData["MsgType"] = "4";
            }
            catch (Exception e)
            {
                TempData["Message"] = "Error Occured while registering the new routcccccc. Please try again!";
                TempData["MsgType"] = "4";

                Console.WriteLine(e);
            }

            return RedirectToAction(nameof(Rout));
        }

        public async Task<ActionResult> ViewNewRout(int rout)
        {
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT rout_no,town FROM rout WHERE status = 1 AND rout_no = @routid";
            cmd.Parameters.AddWithValue("@routid", rout);

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new RoutModel
                    {
                        RoutId = reader.GetFieldValue<int>(0),
                        Town = reader.GetFieldValue<string>(1)
                    };

                    TempData["Message"] = "New distributor registered!";
                    TempData["MsgType"] = "2";
                    return View(temp);
                }
            }

            TempData["Message"] = "Unable to view new distributor!";
            TempData["MsgType"] = "4";
            return RedirectToAction(nameof(Rout));
        }

        //PURCHASE ORDER
        private async Task<List<SupplierMaterialModel>> GetSupMaterial(string supid = null)
        {
            var tempSup = new List<SupplierMaterialModel>();
            var cmd = DBConn.Connection.CreateCommand();
            if (supid == null)
            {
                cmd.CommandText =
                    "SELECT material_supplier.sup_id, supplier.name, material_supplier.rm_id, raw_materials.name, material_supplier.pet, material_supplier.cost, material_supplier.lead_time FROM (material_supplier INNER JOIN supplier ON material_supplier.sup_id = supplier.sup_id) INNER JOIN raw_materials ON material_supplier.rm_id = raw_materials.rm_id ORDER BY material_supplier.sup_id";
            }
            else
            {
                cmd.CommandText =
                    "SELECT material_supplier.sup_id, supplier.name, material_supplier.rm_id, raw_materials.name, material_supplier.pet, material_supplier.cost, material_supplier.lead_time FROM (material_supplier INNER JOIN supplier ON material_supplier.sup_id = supplier.sup_id) INNER JOIN raw_materials ON material_supplier.rm_id = raw_materials.rm_id WHERE material_supplier.sup_id = @supid ORDER BY material_supplier.rm_id";
                cmd.Parameters.AddWithValue("@supid", supid);
            }

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var temp = new SupplierMaterialModel
                {
                    supId = reader.GetFieldValue<int>(0),
                    supName = reader.GetFieldValue<string>(1),
                    rawId = reader.GetFieldValue<int>(2),
                    rawName = reader.GetFieldValue<string>(3),
                    PET = reader.GetFieldValue<int>(4),
                    Cost = reader.GetFieldValue<decimal>(5),
                    lead = reader.GetFieldValue<decimal>(6)
                };

                tempSup.Add(temp);
            }

            return tempSup;
        }

        public async Task<ActionResult> PurchaseOrder()
        {
            var output = new List<PurchaseOrderModel>();
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT purchase_order.po_id,purchase_order.sup_id,name,date,edd,purchase_order.status, SUM(material_purchase.cost*material_purchase.qty) FROM (purchase_order INNER JOIN supplier ON purchase_order.sup_id = supplier.sup_id) INNER JOIN material_purchase ON purchase_order.po_id = material_purchase.po_id GROUP BY purchase_order.po_id";
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new PurchaseOrderModel
                    {
                        poid = reader.GetFieldValue<int>(0), sup = reader.GetFieldValue<int>(1),
                        name = reader.GetFieldValue<string>(2),
                        Date = reader.GetFieldValue<DateTime>(3).ToString("d"),
                        EDD = reader.GetFieldValue<DateTime>(4).ToString("d"),
                        Status = reader.GetFieldValue<int>(5) == 1 ? "Pending" : "Received",
                        Cost = reader.GetFieldValue<decimal>(6)
                    };
                    output.Add(temp);
                }
            }

            return View(output);
        }

        public async Task<ActionResult> CreateNewPurchaseOrder()
        {
            var tempRaw = await GetRawMaterials(1);
            var tempSup = await GetSupMaterial();

            ViewBag.Raw = tempRaw;
            ViewBag.Sup = tempSup;
            return View();
        }

        public async Task<ActionResult> CreateNewPurchaseOrderResult(IFormCollection collection)
        {
            var order = new List<decimal[]>();
            var tempRaw = await GetRawMaterials(1);
            var tempSup = await GetSupMaterial(collection["supID"]);
            var cmd = DBConn.Connection.CreateCommand();

            string mailTable = null;
            decimal lead = 0, total = 0;
            var qty = 0;

            foreach (var item in tempRaw)
            {
                qty = int.Parse(collection[item.Id.ToString()]);
                if (qty > 0)
                    foreach (var item2 in tempSup)
                        if (item2.rawId == item.Id)
                        {
                            order.Add(new[] {item.Id, qty, item2.Cost, item2.lead});
                            mailTable += "<tr> <td>" + item.Id + "</td>" + "<td>" + item.Name + "</td>" + "<td>" +
                                         item2.Cost + "</td>" + "<td>" + qty + "</td>" + "<td>" + item2.Cost * qty +
                                         "</td> </tr>";
                            lead = lead + item2.lead / 1000 * qty;
                            total += item2.Cost * qty;
                            break;
                        }
            }

            var edd = DateTime.Today.AddDays(decimal.ToDouble(lead));
            Console.WriteLine("Inserting PO...");
            cmd.CommandText = "INSERT INTO purchase_order(sup_id,edd) VALUES (@sup, @edd)";
            cmd.Parameters.AddWithValue("@sup", collection["supID"]);
            cmd.Parameters.AddWithValue("@edd", edd);
            var recsMain = cmd.ExecuteNonQuery();
            var poID = 0;

            if (recsMain > 0)
            {
                Console.WriteLine("PO Inserted");
                Console.WriteLine("Taking PO number...");
                cmd.CommandText = "SELECT MAX(po_id) FROM purchase_order";
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    poID = reader.GetFieldValue<int>(0);
                    Console.WriteLine("PO Number taken");
                    break;
                }
            }
            else
            {
                TempData["Message"] = "Couldn't create a purchase order";
                TempData["MsgType"] = "4";
                return RedirectToAction(nameof(PurchaseOrder));
            }

            foreach (var item in order)
                try
                {
                    cmd.Parameters.Clear();
                    Console.WriteLine("Inserting material : " + item[0] + " with QTY : " + item[1]);

                    cmd.CommandText = "INSERT INTO material_purchase VALUES (@rmid, @poid, @cost, @qty)";
                    cmd.Parameters.AddWithValue("@rmid", item[0]);
                    cmd.Parameters.AddWithValue("@poid", poID);
                    cmd.Parameters.AddWithValue("@cost", item[2]);
                    cmd.Parameters.AddWithValue("@qty", item[1]);
                    cmd.ExecuteNonQuery();

                    cmd.Parameters.Clear();
                    cmd.CommandText =
                        "UPDATE raw_materials SET request=request+@qty, req_date=@rqdate  WHERE rm_id = @rmid";
                    cmd.Parameters.AddWithValue("@rmid", item[0]);
                    cmd.Parameters.AddWithValue("@rqdate", DateTime.Today);
                    cmd.Parameters.AddWithValue("@qty", item[1]);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    TempData["Message"] = "Couldn't create a purchase order";
                    TempData["MsgType"] = "4";
                    return RedirectToAction(nameof(PurchaseOrder));
                }

            try
            {
                var message = new MimeMessage();
                var from = new MailboxAddress("SRIMAK", "srimak101@gmail.com");
                message.From.Add(from);

                var to = new MailboxAddress("Test User 2", "saubhagyaudani123@gmail.com");
                message.To.Add(to);

                message.Subject = "New Purchase Order " + poID;

                var bb = new BodyBuilder();
                bb.HtmlBody = "<h3>Purchase Order ID : " + poID + "</h3>" +
                              "<table> <thead> <tr> " +
                              "<th>ID</th> <th>Material</th> <th>Per Unit</th> <th>QTY</th> <th>Cost</th> " +
                              "</tr> </thead>" +
                              "<tbody>" + mailTable + "</tbody>" +
                              "</table>" +
                              "<h4>Total Cost : Rs." + total + "</h4>" +
                              "<h4>Estimated Arrival Date : " + edd.ToString("D") + "</h4>";

                message.Body = bb.ToMessageBody();

                var client = new SmtpClient();
                client.Connect("smtp.gmail.com", 465, true);
                client.Authenticate("srimak101@gmail.com", "sri123456789");
                client.Send(message);
                client.Disconnect(true);
                client.Dispose();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                TempData["Message"] = "An error occured while sending the email";
                TempData["MsgType"] = "4";
                return RedirectToAction(nameof(DetailedPurchaseOrder));
            }

            TempData["Message"] = "Purchase order created";
            TempData["MsgType"] = "2";
            return RedirectToAction(nameof(DetailedPurchaseOrder));
        }

        public async Task<ActionResult> DetailedPurchaseOrder(int id = 0)
        {
            var poDetails = new List<PurchaseOrderMaterialsModel>();
            var cmd = DBConn.Connection.CreateCommand();
            if (id == 0)
            {
                cmd.CommandText =
                    "SELECT material_purchase.rm_id, name, cost, material_purchase.qty FROM material_purchase INNER JOIN raw_materials ON material_purchase.rm_id = raw_materials.rm_id WHERE po_id = (SELECT MAX(po_id) FROM purchase_order)";
            }
            else
            {
                cmd.CommandText =
                    "SELECT material_purchase.rm_id, name, cost, material_purchase.qty FROM material_purchase INNER JOIN raw_materials ON material_purchase.rm_id = raw_materials.rm_id WHERE po_id = @poid";
                cmd.Parameters.AddWithValue("@poid", id);
            }

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new PurchaseOrderMaterialsModel
                    {
                        ID = reader.GetFieldValue<int>(0),
                        Name = reader.GetFieldValue<string>(1),
                        perCost = reader.GetFieldValue<decimal>(2),
                        Cost = reader.GetFieldValue<decimal>(2) * reader.GetFieldValue<int>(3),
                        QTY = reader.GetFieldValue<int>(3)
                    };
                    poDetails.Add(temp);
                }
            }

            return View(poDetails);
        }

        public async Task<ActionResult> SetReceived(int id)
        {
            var matList = new List<int[]>();
            var cmd = DBConn.Connection.CreateCommand();

            cmd.CommandText = "SELECT rm_id, qty FROM material_purchase WHERE po_id = @poid";
            cmd.Parameters.AddWithValue("@poid", id);
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                    matList.Add(new[] {reader.GetFieldValue<int>(0), reader.GetFieldValue<int>(1)});
            }

            foreach (var item in matList)
            {
                cmd.Parameters.Clear();
                cmd.CommandText = "UPDATE raw_materials SET request=request-@qty WHERE rm_id = @rmid";
                cmd.Parameters.AddWithValue("@qty", item[1]);
                cmd.Parameters.AddWithValue("@rmid", item[0]);
                cmd.ExecuteNonQuery();
            }

            cmd.Parameters.Clear();
            cmd.CommandText = "UPDATE purchase_order SET status = 2 WHERE po_id = @poid";
            cmd.Parameters.AddWithValue("@poid", id);
            cmd.ExecuteNonQuery();

            TempData["Message"] = "Materials received";
            TempData["MsgType"] = "2";
            return RedirectToAction(nameof(PurchaseOrder));
        }

        //DAILY INDOOR TEST
        public async Task<ActionResult> DailyIndoorTest()
        {
            var output = new List<DailyIndoorTestModel>();
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT test_code, tested_date, result FROM qulity_test WHERE type = 1 ORDER BY tested_date DESC";
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new DailyIndoorTestModel
                    {
                        Code = reader.GetFieldValue<string>(0),
                        Date = reader.GetFieldValue<DateTime>(1).ToString("yyyy MMMM dd"),
                        Result = reader.GetFieldValue<int>(2)
                    };

                    output.Add(temp);
                }
            }

            cmd.CommandText = "SELECT test_code FROM qulity_test WHERE DATE(tested_date) = CURDATE() AND type = 1";
            var reader2 = cmd.ExecuteReader();

            if (reader2.HasRows)
                ViewBag.update = 1;
            else
                ViewBag.update = 0;

            SetActiveNavbar(10);
            return View(output);
        }

        public ActionResult CreateNewDailyIndoorTest()
        {
            ViewBag.code = "DIT" + DateTime.Now.ToString("yyMMdd");
            return View();
        }

        public ActionResult CreateNewDailyIndoorTestResult(IFormCollection collection)
        {
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "INSERT INTO qulity_test(test_code, result, ph_level, hardness, fe_comp, type) VALUES (@code, @result, @ph, @hard, @fe, 1)";
            cmd.Parameters.AddWithValue("@code", "DIT" + DateTime.Now.ToString("yyMMdd"));
            cmd.Parameters.AddWithValue("@result", collection["result"]);
            cmd.Parameters.AddWithValue("@ph", collection["PH"]);
            cmd.Parameters.AddWithValue("@hard", collection["Hardness"]);
            cmd.Parameters.AddWithValue("@fe", collection["fe"]);
            var recs = cmd.ExecuteNonQuery();

            if (recs > 0)
            {
                TempData["Message"] = "Today's daily indoor test recorded";
                TempData["MsgType"] = "2";
                return RedirectToAction(nameof(DailyIndoorTest));
            }

            TempData["Message"] = "Couldn't record today's daily indoor test. Please try again";
            TempData["MsgType"] = "4";
            return RedirectToAction(nameof(CreateNewDailyIndoorTest));
        }

        public async Task<ActionResult> UpdateDailyIndoorTest()
        {
            var output = new DailyIndoorTestModel();
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText = "SELECT ph_level,hardness,fe_comp FROM qulity_test WHERE test_code = @code";
            cmd.Parameters.AddWithValue("@code", "DIT" + DateTime.Now.ToString("yyMMdd"));

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    output.PH = reader.GetFieldValue<double>(0);
                    output.Hardness = reader.GetFieldValue<double>(1);
                    output.fe = reader.GetFieldValue<double>(2);
                }
            }

            return View(output);
        }

        public ActionResult UpdateDailyIndoorTestResult(IFormCollection collection)
        {
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "UPDATE qulity_test SET result = @result, ph_level = @ph, hardness = @hard, fe_comp = @fe WHERE test_code = @code";
            cmd.Parameters.AddWithValue("@code", "DIT" + DateTime.Now.ToString("yyMMdd"));
            cmd.Parameters.AddWithValue("@result", collection["result"]);
            cmd.Parameters.AddWithValue("@ph", collection["PH"]);
            cmd.Parameters.AddWithValue("@hard", collection["Hardness"]);
            cmd.Parameters.AddWithValue("@fe", collection["fe"]);

            var recs = cmd.ExecuteNonQuery();

            if (recs > 0)
            {
                TempData["Message"] = "Today's daily indoor test updated";
                TempData["MsgType"] = "2";
                return RedirectToAction(nameof(DailyIndoorTest));
            }

            TempData["Message"] = "Couldn't update today's daily indoor test. Please try again";
            TempData["MsgType"] = "4";
            return RedirectToAction(nameof(CreateNewDailyIndoorTest));
        }

        public async Task<ActionResult> ViewDailyIndoorTest(string id)
        {
            var output = new DailyIndoorTestModel();
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT test_code, tested_date, result, ph_level, hardness, fe_comp FROM qulity_test WHERE test_code = @code";
            cmd.Parameters.AddWithValue("@code", id);
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    output.Code = reader.GetFieldValue<string>(0);
                    output.Date = reader.GetFieldValue<DateTime>(1).ToString("D");
                    output.Result = reader.GetFieldValue<int>(2);
                    output.PH = reader.GetFieldValue<double>(3);
                    output.Hardness = reader.GetFieldValue<double>(4);
                    output.fe = reader.GetFieldValue<double>(5);
                }
            }

            return View(output);
        }

        //MONTHLY TEST
        public async Task<ActionResult> MicroTest()
        {
            var output = new List<MicroTestModel>();
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText = "SELECT test_code, tested_date, result FROM qulity_test WHERE type = 2";
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new MicroTestModel
                    {
                        Code = reader.GetFieldValue<string>(0),
                        Date = reader.GetFieldValue<DateTime>(1).ToString("yyyy MMMM dd"),
                        Result = reader.GetFieldValue<int>(2)
                    };

                    output.Add(temp);
                }
            }

            cmd.CommandText =
                "SELECT test_code FROM qulity_test WHERE MONTH(tested_date) = MONTH(CURRENT_DATE()) AND YEAR(tested_date) = YEAR(CURRENT_DATE()) AND type = 2";
            var reader2 = cmd.ExecuteReader();

            if (reader2.HasRows)
                ViewBag.update = 1;
            else
                ViewBag.update = 0;

            SetActiveNavbar(11);
            return View(output);
        }

        public ActionResult CreateNewMicroTest()
        {
            ViewBag.code = "MBT" + DateTime.Now.ToString("yyMM");
            return View();
        }

        public ActionResult CreateNewMicroTestResult(IFormCollection collection)
        {
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "INSERT INTO qulity_test(test_code, result, ecoil_count, coilform_cnt_well, coilform_cnt_fnl, type) VALUES (@code, @result, @ecoli, @coliWell, @coliFnl, 2)";
            cmd.Parameters.AddWithValue("@code", "MBT" + DateTime.Now.ToString("yyMM"));
            cmd.Parameters.AddWithValue("@result", collection["result"]);
            cmd.Parameters.AddWithValue("@ecoli", collection["ecoli"]);
            cmd.Parameters.AddWithValue("@coliWell", collection["colWell"]);
            cmd.Parameters.AddWithValue("@coliFnl", collection["colFinal"]);
            var recs = cmd.ExecuteNonQuery();

            if (recs > 0)
            {
                TempData["Message"] = "Current month's microbiology test recorded";
                TempData["MsgType"] = "2";
                return RedirectToAction(nameof(MicroTest));
            }

            TempData["Message"] = "Couldn't record current month's microbiology test. Please try again";
            TempData["MsgType"] = "4";
            return RedirectToAction(nameof(CreateNewMicroTest));
        }

        public async Task<ActionResult> UpdateMicroTest()
        {
            var output = new MicroTestModel();
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT ecoil_count, coilform_cnt_well, coilform_cnt_fnl FROM qulity_test WHERE test_code = @code";
            cmd.Parameters.AddWithValue("@code", "MBT" + DateTime.Now.ToString("yyMM"));

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    output.ecoli = reader.GetFieldValue<double>(0);
                    output.colWell = reader.GetFieldValue<double>(1);
                    output.colFinal = reader.GetFieldValue<double>(2);
                }
            }

            return View(output);
        }

        public ActionResult UpdateMicroTestResult(IFormCollection collection)
        {
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "UPDATE qulity_test SET result = @result, ecoil_count = @ecoli, coilform_cnt_well = @coliWell, coilform_cnt_fnl = @coliFnl WHERE test_code = @code";
            cmd.Parameters.AddWithValue("@code", "MBT" + DateTime.Now.ToString("yyMM"));
            cmd.Parameters.AddWithValue("@result", collection["result"]);
            cmd.Parameters.AddWithValue("@ecoli", collection["ecoli"]);
            cmd.Parameters.AddWithValue("@coliWell", collection["colWell"]);
            cmd.Parameters.AddWithValue("@coliFnl", collection["colFinal"]);

            var recs = cmd.ExecuteNonQuery();

            if (recs > 0)
            {
                TempData["Message"] = "Current month's microbiology test updated";
                TempData["MsgType"] = "2";
                return RedirectToAction(nameof(MicroTest));
            }

            TempData["Message"] = "Couldn't update current month's microbiology test. Please try again";
            TempData["MsgType"] = "4";
            return RedirectToAction(nameof(CreateNewMicroTest));
        }

        public async Task<ActionResult> ViewMicroTest(string id)
        {
            var output = new MicroTestModel();
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT test_code, tested_date, result, ecoil_count, coilform_cnt_well, coilform_cnt_fnl FROM qulity_test WHERE test_code = @code";
            cmd.Parameters.AddWithValue("@code", id);

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    output.Code = reader.GetFieldValue<string>(0);
                    output.Date = reader.GetFieldValue<DateTime>(1).ToString("D");
                    output.Result = reader.GetFieldValue<int>(2);
                    output.ecoli = reader.GetFieldValue<double>(3);
                    output.colWell = reader.GetFieldValue<double>(4);
                    output.colFinal = reader.GetFieldValue<double>(5);
                }
            }

            return View(output);
        }

        //Reports
        public async Task<ActionResult> Reports()
        {
            var dailyProductionList = new List<DailyProductionModel>();
            var salesList = new List<FinishedProductModel>();
            var dalilyIndoorList = new List<DailyIndoorTestModel>();

            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT name, SUM(prod) AS Prod, SUM(wast) AS Wast FROM daily_production INNER JOIN finished_product ON daily_production.rm_id = finished_product.rm_id WHERE YEAR(date) = YEAR(CURRENT_DATE - INTERVAL 1 MONTH) AND MONTH(date) = MONTH(CURRENT_DATE - INTERVAL 1 MONTH) GROUP BY name";
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new DailyProductionModel()
                    {
                        Name = reader.GetFieldValue<string>(0),
                        Production = Convert.ToInt16(reader.GetFieldValue<decimal>(1)),
                        Wastage = Convert.ToInt16(reader.GetFieldValue<decimal>(2))
                    };

                    dailyProductionList.Add(temp);
                }
            }

            cmd.CommandText =
                "SELECT name, SUM(lst_qty) AS qty FROM ((SELECT pro_id, IF(new_qty>0, new_qty, qty) AS lst_qty, so_id FROM sales_product) AS Temp INNER JOIN finished_product ON Temp.pro_id = finished_product.pro_id) INNER JOIN sales_order ON sales_order.so_id = Temp.so_id WHERE YEAR(date) = YEAR(CURRENT_DATE - INTERVAL 1 MONTH) AND MONTH(date) = MONTH(CURRENT_DATE - INTERVAL 1 MONTH) GROUP BY name";
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new FinishedProductModel()
                    {
                        Name = reader.GetFieldValue<string>(0),
                        QTY = Convert.ToInt16(reader.GetFieldValue<decimal>(1))
                    };

                    salesList.Add(temp);
                }
            }

            cmd.CommandText = "SELECT tested_date, ph_level, hardness, fe_comp FROM qulity_test WHERE YEAR(tested_date) = YEAR(CURRENT_DATE - INTERVAL 1 MONTH) AND MONTH(tested_date) = MONTH(CURRENT_DATE - INTERVAL 1 MONTH) AND type = 1 ORDER BY tested_date";
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new DailyIndoorTestModel()
                    {
                        Code = "DIT" + reader.GetFieldValue<DateTime>(0).ToString("yyMMdd"),
                        PH = reader.GetFieldValue<double>(1),
                        Hardness = reader.GetFieldValue<double>(2),
                        fe = reader.GetFieldValue<double>(3)
                    };

                    dalilyIndoorList.Add(temp);
                }
            }

            ViewBag.prvMonth = DateTime.Now.AddMonths(-1).ToString("yyyy MMMM");
            ViewBag.Qulity = dalilyIndoorList;
            ViewBag.Production = dailyProductionList;
            ViewBag.Sales = salesList;
            SetActiveNavbar(12);
            return View();
        }

        //SALES ORDER
        public async Task<ActionResult> SalesOrder()
        {
            var output = new List<SalesOrderModel>();
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT so_id, date, due_date, user.name, distributor.name, sales_order.status FROM (sales_order INNER JOIN user ON sales_order.user_id = user.user_id) LEFT JOIN distributor ON sales_order.dis_id = distributor.dis_id ORDER BY sales_order.status, date, date";
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new SalesOrderModel()
                    {
                        soID = reader.GetFieldValue<int>(0),
                        date = reader.GetFieldValue<DateTime>(1).ToString("d"),
                        dueDate = reader.GetFieldValue<DateTime>(2).ToString("d"),
                        userName = reader.GetFieldValue<string>(3),
                        disName = reader.GetValue(4) is DBNull ? "-" : reader.GetFieldValue<string>(4),
                        Status = reader.GetFieldValue<int>(5)
                    };

                    output.Add(temp);
                }
            }

            SetActiveNavbar(13);
            return View(output);
        }
        public async Task<ActionResult> ViewSalesOrder(int id)
        {
            var isNewQty = 0;
            var output = new List<SalesOrderModel>();
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText = "SELECT sales_product.pro_id, finished_product.name, sales_product.qty, new_qty, sales_product.cost, sales_order.status FROM (sales_product INNER JOIN finished_product ON sales_product.pro_id = finished_product.pro_id) INNER JOIN sales_order ON sales_product.so_id=sales_order.so_id WHERE sales_product.so_id = @soid";
            cmd.Parameters.AddWithValue("@soid", id);
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new SalesOrderModel()
                    {
                        proID = reader.GetFieldValue<int>(0),
                        prod = reader.GetFieldValue<string>(1),
                        QTY = reader.GetFieldValue<int>(2),
                        newQTY = reader.GetFieldValue<int>(3),
                        Cost = reader.GetFieldValue<decimal>(4)
                    };

                    ViewBag.status = reader.GetFieldValue<int>(5);
                    if (isNewQty == 0 && reader.GetFieldValue<int>(3) > 0)
                    {
                        isNewQty = 1;
                    }
                    output.Add(temp);
                }
            }

            if (ViewBag.status == 2)
            {
                var tempDis = new List<DistributorModel>();
                cmd.CommandText = "SELECT dis_id, name, vehi_type FROM distributor";
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var temp = new DistributorModel()
                    {
                        dis_id = reader.GetFieldValue<int>(0),
                        Name = reader.GetFieldValue<string>(1),
                        vehi_type = reader.GetFieldValue<string>(2)
                    };

                    tempDis.Add(temp);
                }

                ViewBag.disList = tempDis;
            }
            else if (ViewBag.status == 3 || ViewBag.status == 4)
            {
                var tempDis = new DistributorModel();
                cmd.CommandText = "SELECT dis_id, name, email, contact, vehi_no, vehi_type, distributor.rout_no, town FROM distributor INNER JOIN rout ON distributor.rout_no=rout.rout_no WHERE dis_id=(SELECT dis_id FROM sales_order WHERE so_id=@soid)";
                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    tempDis.dis_id = reader.GetFieldValue<int>(0);
                    tempDis.Name = reader.GetFieldValue<string>(1);
                    tempDis.Email = reader.GetFieldValue<string>(2);
                    tempDis.Contact = reader.GetFieldValue<string>(3);
                    tempDis.vehi_no = reader.GetFieldValue<string>(4);
                    tempDis.vehi_type = reader.GetFieldValue<string>(5);
                    tempDis.Rout = reader.GetFieldValue<int>(6);
                    tempDis.Town = reader.GetFieldValue<string>(7);
                }

                ViewBag.disList = tempDis;
            }

            TempData["soID"] = id;
            ViewBag.newQTY = isNewQty;
            return View(output);
        }

        //CONSUMPTION
        public async Task<ActionResult> Consumption()
        {
            var outputMonth = new List<DailyProductionModel>();

            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT date, daily_production.rm_id,name, prod, wast FROM daily_production INNER JOIN raw_materials ON daily_production.rm_id=raw_materials.rm_id ORDER BY date DESC";
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new DailyProductionModel
                    {
                        Date = reader.GetFieldValue<DateTime>(0),
                        rmID = reader.GetFieldValue<int>(1),
                        Name = reader.GetFieldValue<string>(2),
                        Production = reader.GetFieldValue<int>(3),
                        Wastage = reader.GetFieldValue<int>(4)
                    };

                    outputMonth.Add(temp);
                }
            }

            var waterConsumptionList = new List<DailyProductionModel>(); 
            cmd.CommandText =
                "SELECT date, SUM((prod*rm_size)/1000) FROM daily_production INNER JOIN raw_materials ON daily_production.rm_id = raw_materials.rm_id GROUP BY date";
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new DailyProductionModel()
                    {
                        stringDate = reader.GetFieldValue<DateTime>(0).ToString("yy-MMM-dd"),
                        Production = decimal.ToInt32(reader.GetFieldValue<decimal>(1))
                    };

                    waterConsumptionList.Add(temp);
                }
            }

            ViewBag.Water = waterConsumptionList;
            SetActiveNavbar(14);
            return View(outputMonth);
        }

        //MISC
        private async Task<List<RawMaterialModel>> GetRawMaterials(int x = 0, string pram1 = "rm_id",
            string pram2 = null, int supid = 0)
        {
            try
            {
                var output = new List<RawMaterialModel>();
                var cmd = DBConn.Connection.CreateCommand();
                if (x == 1)
                {
                    cmd.CommandText = "SELECT * FROM raw_materials WHERE status = 1 OR status = 3";
                }
                else if (x == 2)
                {
                    cmd.CommandText = "SELECT * FROM raw_materials WHERE status = 1 OR status = 3 ORDER BY " + pram1 +
                                      " " + pram2;
                }
                else if (x == 3)
                {
                    cmd.CommandText =
                        "SELECT * FROM raw_materials WHERE status = 1 or status = 3 AND rm_id NOT IN (SELECT rm_id FROM material_supplier WHERE sup_id = @supid)";
                    cmd.Parameters.AddWithValue("@supid", supid);
                }
                else
                {
                    cmd.CommandText =
                        "SELECT * FROM raw_materials WHERE (qty<=rol OR request>0 OR (qty*100/rol)-100<5) AND status = 1 or status = 3";
                }

                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var tempDate = "";

                        if (reader.GetDateTime(9) == DateTime.Parse("0001-1-1"))
                            tempDate = "-";
                        else
                            tempDate = reader.GetDateTime(9).ToString("MMMM dd, yyyy");

                        var temp = new RawMaterialModel
                        {
                            Id = reader.GetFieldValue<int>(0),
                            Name = reader.GetFieldValue<string>(1),
                            Size = reader.GetFieldValue<int>(2),
                            QTY = reader.GetFieldValue<int>(3),
                            ROL = reader.GetFieldValue<int>(4),
                            Buffer = reader.GetFieldValue<int>(5),
                            Consumption = reader.GetFieldValue<int>(6),
                            Stock = reader.GetFieldValue<int>(7),
                            Request = reader.GetFieldValue<int>(8),
                            ReqDate = tempDate
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

        private void SetActiveNavbar(int x)
        {
            switch (x)
            {
                case 1:
                    ViewData["Dashboard"] = "active";
                    break;

                case 2:
                    ViewData["Distributor"] = "active";
                    break;

                case 3:
                    ViewData["Materials"] = "active";
                    break;

                case 4:
                    ViewData["Products"] = "active";
                    break;

                case 5:
                    ViewData["Resellers"] = "active";
                    break;

                case 6:
                    ViewData["Suppliers"] = "active";
                    break;

                case 7:
                    ViewData["Purchase"] = "active";
                    break;

                case 8:
                    ViewData["Rout"] = "active";
                    break;

                case 9:
                    ViewData["Clerk"] = "active";
                    break;

                case 10:
                    ViewData["Daily"] = "active";
                    break;

                case 11:
                    ViewData["Monthly"] = "active";
                    break;

                case 12:
                    ViewData["Report"] = "active";
                    break;

                case 13:
                    ViewData["Sales"] = "active";
                    break;

                case 14:
                    ViewData["Consumption"] = "active";
                    break;
            }
        }

        private bool CheckSession()
        {
            if (HttpContext.Session.GetString("Name") != null)
                return false;
            return false;
        }

        private int GetTemp()
        {
            //ARDUINO CODE
            //CommunicatorModel comport = new CommunicatorModel();
            //if (comport.connect(9600, "I'M ARDUINO", 4, 8, 16))
            //{
            //    return int.Parse(comport.message(4, 8, 32) + "C");
            //}
            //else
            //{
            //    return -1;
            //}

            return new Random().Next(20 , 40);
        }
    }
}