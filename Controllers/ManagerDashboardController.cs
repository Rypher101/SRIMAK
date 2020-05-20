using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
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

        // GET: ManagerDashboard
        public async Task<ActionResult> Index()
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            var RawMaterial = await GetRawMaterials();

            ViewData["MatCount"] = RawMaterial.Count;
            SetActiveNavbar(1);

            return View(RawMaterial);
        }

        // DASHBOARD
        private async Task<List<RawMaterialModel>> GetRawMaterials(int x = 0, string pram1 = "rm_id",
            string pram2 = null, int supid = 0)
        {
            try
            {
                var output = new List<RawMaterialModel>();
                var cmd = DBConn.Connection.CreateCommand();
                if (x == 1)
                {
                    cmd.CommandText = "SELECT * FROM raw_materials WHERE status = 1";
                }
                else if (x == 2)
                {
                    cmd.CommandText = "SELECT * FROM raw_materials WHERE status = 1 ORDER BY " + pram1 + " " + pram2;
                }
                else if (x == 3)
                {
                    cmd.CommandText =
                        "SELECT * FROM raw_materials WHERE status = 1 AND rm_id NOT IN (SELECT rm_id FROM material_supplier WHERE sup_id = @supid)";
                    cmd.Parameters.AddWithValue("@supid", supid);
                }
                else
                {
                    cmd.CommandText = "SELECT * FROM raw_materials WHERE (qty<=rol OR request>0) AND status = 1";
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

        //MATERIAL
        public async Task<ActionResult> Materials(string sortPram = null, string typePram = null)
        {
            //Session check
            if (CheckSession())
                return RedirectToAction("Index", "Login", new {id = 1});
            TempData["User"] = HttpContext.Session.GetString("Name");

            Debug.WriteLine(sortPram);
            Debug.WriteLine(typePram);

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
            cmd.CommandText = "SELECT purchase_order.po_id,purchase_order.sup_id,name,date,edd,purchase_order.status, SUM(material_purchase.cost*material_purchase.qty) FROM (purchase_order INNER JOIN supplier ON purchase_order.sup_id = supplier.sup_id) INNER JOIN material_purchase ON purchase_order.po_id = material_purchase.po_id GROUP BY purchase_order.po_id";
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new PurchaseOrderModel()
                    {
                        poid = reader.GetFieldValue<int>(0), sup = reader.GetFieldValue<int>(1),
                        name = reader.GetFieldValue<string>(2), 
                        Date = reader.GetFieldValue<DateTime>(3).ToString("d"), 
                        EDD = reader.GetFieldValue<DateTime>(4).ToString("d"), 
                        Status = reader.GetFieldValue<int>(5)==1 ? "Pending" : "Received", 
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
                            mailTable += "<tr> <td>"+ item.Id+"</td>" + "<td>" + item.Name + "</td>" + "<td>" + item2.Cost + "</td>" + "<td>" + qty + "</td>" + "<td>" + item2.Cost * qty + "</td> </tr>";
                            lead = lead+((item2.lead / 1000) * qty);
                            total += item2.Cost * qty;
                            break;
                        }
            }

            var edd = DateTime.Today.AddDays(Decimal.ToDouble(lead));
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
                MimeMessage message = new MimeMessage();
                MailboxAddress from = new MailboxAddress("SRIMAK","srimak101@gmail.com");
                message.From.Add(from);

                MailboxAddress to = new MailboxAddress("Test User 2","saubhagyaudani123@gmail.com");
                message.To.Add(to);

                message.Subject = "New Purchase Order " + poID;

                BodyBuilder bb = new BodyBuilder();
                bb.HtmlBody = "<h3>Purchase Order ID : " + poID + "</h3>" +
                    "<table> <thead> <tr> " +
                    "<th>ID</th> <th>Material</th> <th>Per Unit</th> <th>QTY</th> <th>Cost</th> " +
                    "</tr> </thead>" +
                    "<tbody>" + mailTable + "</tbody>" +
                    "</table>" +
                    "<h4>Total Cost : Rs." + total + "</h4>" +
                    "<h4>Estimated Arrival Date : " + edd.ToString("D") + "</h4>";

                message.Body = bb.ToMessageBody();

                SmtpClient client = new SmtpClient();
                client.Connect("smtp.gmail.com",465 , true);
                client.Authenticate("srimak101@gmail.com" , "sri123456789");
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
                cmd.CommandText = "SELECT material_purchase.rm_id, name, cost, material_purchase.qty FROM material_purchase INNER JOIN raw_materials ON material_purchase.rm_id = raw_materials.rm_id WHERE po_id = (SELECT MAX(po_id) FROM purchase_order)";
            }
            else
            {
                cmd.CommandText = "SELECT material_purchase.rm_id, name, cost, material_purchase.qty FROM material_purchase INNER JOIN raw_materials ON material_purchase.rm_id = raw_materials.rm_id WHERE po_id = @poid";
                cmd.Parameters.AddWithValue("@poid", id);
            }

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new PurchaseOrderMaterialsModel()
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

        //add this to clerk controller
        public async Task<ActionResult> SetReceived(int id)
        {
            List<int[]> matList = new List<int[]>();
            var cmd = DBConn.Connection.CreateCommand();

            cmd.CommandText = "SELECT rm_id, qty FROM material_purchase WHERE po_id = @poid";
            cmd.Parameters.AddWithValue("@poid", id);
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    matList.Add(new []{reader.GetFieldValue<int>(0) , reader.GetFieldValue<int>(1)});
                }
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

        //MISC
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
            }
        }

        private bool CheckSession()
        {
            if (HttpContext.Session.GetString("Name") != null)
                return false;
            return false;
        }
    }
}