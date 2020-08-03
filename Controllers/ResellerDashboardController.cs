using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SRIMAK.Models;

namespace SRIMAK.Controllers
{
    public class ResellerDashboardController : Controller
    {
        public ResellerDashboardController(DBConnection DB)
        {
            DBConn = DB;
        }

        private DBConnection DBConn { get; }

        // GET: ResellerDashboard
        public async Task<ActionResult> Index()
        {
            var cmd = DBConn.Connection.CreateCommand();
            //cmd.CommandText =
            //    "SELECT user_id,name,doa,contact,address,type,email,rout,town FROM user INNER JOIN rout ON user.rout = rout.rout_no WHERE user_id = @user";
            cmd.CommandText = "SELECT password FROm user WHERE user_id = @user";
            cmd.Parameters.AddWithValue("@user", HttpContext.Session.GetString("UID"));

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                    if (reader.GetFieldValue<string>(0) ==
                        "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918")
                    {
                        TempData["Message"] =
                            "You are still using the default password. To change that, Goto Account and assign a new password";
                        TempData["MsgType"] = "4";
                    }
            }

            return View();
        }

        //ACCOUNT
        public async Task<ActionResult> Account()
        {
            ViewBag.Rout = await GetRouts();
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT user.user_id,name,doa,contact_number,address,email,rout,town FROM user INNER JOIN rout ON user.rout = rout.rout_no WHERE user.user_id = @user";
            cmd.Parameters.AddWithValue("@user", HttpContext.Session.GetString("UID"));

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new ResellerModel
                    {
                        userID = reader.GetFieldValue<string>(0),
                        Name = reader.GetFieldValue<string>(1),
                        doa = reader.GetFieldValue<DateTime>(2).ToString("d"),
                        contact = reader.GetFieldValue<string>(3),
                        Address = reader.GetFieldValue<string>(4),
                        Email = reader.GetFieldValue<string>(5),
                        Rout = reader.GetFieldValue<int>(6),
                        Town = reader.GetFieldValue<string>(7)
                    };
                    ViewBag.date = reader.GetFieldValue<DateTime>(2);
                    return View(temp);
                }
            }

            ViewData["Message"] = "Invalid login credentials! Please login again";
            return RedirectToAction("Index", "Login");
        }

        public ActionResult AccountResult(IFormCollection collection)
        {
            var cmd = DBConn.Connection.CreateCommand();
            if (collection["Pass"] != "")
            {
                cmd.CommandText =
                    "UPDATE user SET user_id=@user, password=@password, name=@name, doa=@doa, contact_number=@contact, address=@address, email=@email, rout=@rout WHERE user_id=@prvUser AND type=3";
                cmd.Parameters.AddWithValue("@user", collection["userID"]);
                cmd.Parameters.AddWithValue("@password", ShaEncrypt(collection["Pass"]));
                cmd.Parameters.AddWithValue("@name", collection["Name"]);
                cmd.Parameters.AddWithValue("@doa", collection["doa"]);
                cmd.Parameters.AddWithValue("@contact", collection["contact"]);
                cmd.Parameters.AddWithValue("@address", collection["Address"]);
                cmd.Parameters.AddWithValue("@email", collection["Email"]);
                cmd.Parameters.AddWithValue("@rout", collection["Rout"]);
                cmd.Parameters.AddWithValue("@prvUser", collection["prvUser"]);
            }
            else
            {
                cmd.CommandText =
                    "UPDATE user SET user_id=@user, name=@name, doa=@doa, contact_number=@contact, address=@address, email=@email, rout=@rout WHERE user_id=@prvUser AND type=3";
                cmd.Parameters.AddWithValue("@user", collection["userID"]);
                cmd.Parameters.AddWithValue("@name", collection["Name"]);
                cmd.Parameters.AddWithValue("@doa", collection["doa"]);
                cmd.Parameters.AddWithValue("@contact", collection["contact"]);
                cmd.Parameters.AddWithValue("@address", collection["Address"]);
                cmd.Parameters.AddWithValue("@email", collection["Email"]);
                cmd.Parameters.AddWithValue("@rout", collection["Rout"]);
                cmd.Parameters.AddWithValue("@prvUser", collection["prvUser"]);
            }

            var recs = cmd.ExecuteNonQuery();
            if (recs > 0)
            {
                TempData["Message"] = "User profile updated!";
                TempData["MsgType"] = "2";
                return RedirectToAction(nameof(Account));
            }

            TempData["Message"] = "User profile updated failed!";
            TempData["MsgType"] = "4";
            return RedirectToAction(nameof(Account));
        }

        //SALES ORDER
        public async Task<ActionResult> SalesOrder()
        {
            if (HttpContext.Session.GetString("UID") == null) return RedirectToAction("Index", "Login");

            var output = new List<SalesOrderModel>();
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText = "SELECT so_id, date, due_date, dis_id, status FROM sales_order ORDER BY status";
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var dis = reader.GetValue(3).Equals(DBNull.Value) ? 0 : reader.GetFieldValue<int>(3);
                    var temp = new SalesOrderModel
                    {
                        soID = reader.GetFieldValue<int>(0),
                        date = reader.GetFieldValue<DateTime>(1).ToString("yyyy-M-d dddd"),
                        dueDate = reader.GetFieldValue<DateTime>(2).ToString("yyyy-M-d dddd"),
                        disID = dis,
                        Status = reader.GetFieldValue<int>(4)
                    };

                    output.Add(temp);
                }
            }

            return View(output);
        }

        public async Task<ActionResult> ViewSalesOrder(int id)
        {
            var output = new List<SalesOrderModel>();
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT sales_product.pro_id, name, sales_product.qty, cost FROM sales_product INNER JOIN finished_product ON sales_product.pro_id = finished_product.pro_id WHERE so_id = @soid";
            cmd.Parameters.AddWithValue("@soid", id);
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new SalesOrderModel
                    {
                        proID = reader.GetFieldValue<int>(0),
                        prod = reader.GetFieldValue<string>(1),
                        QTY = reader.GetFieldValue<int>(2),
                        Cost = reader.GetFieldValue<decimal>(3)
                    };

                    output.Add(temp);
                }
            }

            return View(output);
        }

        public async Task<ActionResult> ViewDistributor(int id)
        {
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT dis_id, name, email, contact, vehi_no, vehi_type FROM distributor WHERE status = 1 AND dis_id = @dis";
            cmd.Parameters.AddWithValue("@dis", id);

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new DistributorModel
                    {
                        dis_id = reader.GetFieldValue<int>(0),
                        Name = reader.GetFieldValue<string>(1),
                        Email = reader.GetFieldValue<string>(2),
                        Contact = reader.GetFieldValue<string>(3),
                        vehi_no = reader.GetFieldValue<string>(4),
                        vehi_type = reader.GetFieldValue<string>(5)
                    };

                    return View(temp);
                }
            }

            TempData["Message"] = "Unable to view distributor. Please try again";
            TempData["MsgType"] = "4";
            return RedirectToAction(nameof(SalesOrder));
        }

        public ActionResult DeleteSalesOrderResult(int id)
        {
            try
            {
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText = "DELETE FROM sales_product WHERE so_id = @soid";
                cmd.Parameters.AddWithValue("@soid", id);
                cmd.ExecuteNonQuery();

                cmd.Parameters.Clear();
                cmd.CommandText = "DELETE FROM sales_order WHERE so_id = @soid";
                cmd.Parameters.AddWithValue("@soid", id);
                cmd.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                TempData["Message"] = "Couldn't delete sales Order please try again";
                TempData["MsgType"] = "4";
                return RedirectToAction(nameof(SalesOrder));
            }

            TempData["Message"] = "Sales order deleted";
            TempData["MsgType"] = "2";
            return RedirectToAction(nameof(SalesOrder));
        }

        public async Task<ActionResult> CreateNewSalesOrder()
        {
            if (HttpContext.Session.GetString("UID") == null) return RedirectToAction("Index", "Login");

            var output = new List<FinishedProductModel>();
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM finished_product WHERE status = 1";
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new FinishedProductModel
                    {
                        pro_id = reader.GetFieldValue<int>(0),
                        Name = reader.GetFieldValue<string>(1),
                        Price = reader.GetFieldValue<decimal>(3)
                    };

                    output.Add(temp);
                }

                ViewBag.Model = output;
                return View(output);
            }
        }

        public async Task<ActionResult> CreateNewSalesOrderResult(IFormCollection collection)
        {
            if (HttpContext.Session.GetString("UID") == null) return RedirectToAction("Index", "Login");

            var soID = 0;
            var raw = await GetFinishedProducts();
            var cmd = DBConn.Connection.CreateCommand();

            cmd.CommandText = "INSERT INTO sales_order(due_date, user_id) VALUES (@due, @user)";
            cmd.Parameters.AddWithValue("@due", collection["reqDate"]);
            cmd.Parameters.AddWithValue("@user", HttpContext.Session.GetString("UID"));
            if (cmd.ExecuteNonQuery() == 0)
            {
                TempData["Message"] = "Unable to create sales order. Please try again";
                TempData["MsgType"] = "4";
                return RedirectToAction(nameof(SalesOrder));
            }

            cmd.CommandText = "SELECT MAX(so_id) FROM sales_order";
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    soID = reader.GetFieldValue<int>(0);
                    break;
                }
            }

            if (soID == 0)
            {
                TempData["Message"] = "Unable to create sales order. Please try again";
                TempData["MsgType"] = "4";
                return RedirectToAction(nameof(SalesOrder));
            }

            foreach (var item in raw)
                if (int.Parse(collection[item.pro_id.ToString()]) > 0)
                {
                    cmd.Parameters.Clear();
                    cmd.CommandText = "INSERT INTO sales_product VALUES (@so, @pro, @qty, @cost)";
                    cmd.Parameters.AddWithValue("@so", soID);
                    cmd.Parameters.AddWithValue("@pro", item.pro_id);
                    cmd.Parameters.AddWithValue("@qty", collection[item.pro_id.ToString()]);
                    cmd.Parameters.AddWithValue("@cost", item.Price);
                    cmd.ExecuteNonQuery();
                }

            TempData["Message"] = "Sales order placed";
            TempData["MsgType"] = "2";
            return RedirectToAction(nameof(SalesOrder));
        }

        //MISC
        private async Task<List<FinishedProductModel>> GetFinishedProducts()
        {
            var output = new List<FinishedProductModel>();
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM finished_product WHERE status = 1";
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var temp = new FinishedProductModel
                {
                    pro_id = reader.GetFieldValue<int>(0),
                    Name = reader.GetFieldValue<string>(1),
                    Price = reader.GetFieldValue<decimal>(3)
                };
                output.Add(temp);
            }

            return output;
        }

        private async Task<List<RawMaterialModel>> GetRawMaterials(int x = 0, string pram1 = "rm_id",
            string pram2 = null, int supid = 0)
        {
            try
            {
                var output = new List<RawMaterialModel>();
                var cmd = DBConn.Connection.CreateCommand();
                if (x == 0)
                {
                    cmd.CommandText = "SELECT * FROM raw_materials WHERE status = 1";
                }
                else
                {
                    cmd.CommandText =
                        "SELECT * FROM raw_materials WHERE status = 1 AND rm_id NOT IN (SELECT rm_id FROM material_supplier WHERE sup_id = @supid)";
                    cmd.Parameters.AddWithValue("@supid", supid);
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

        private static string ShaEncrypt(string input)
        {
            using var sha256 = HashAlgorithm.Create("sha256");
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            var hash = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            return hash;
        }
    }
}