using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SRIMAK.Models;
using SshNet.Security.Cryptography;

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
                {
                    if (reader.GetFieldValue<string>(0) ==
                        "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918")
                    {
                        TempData["Message"] =
                            "You are still using the default password. To change that, Goto Account and assign a new password";
                        TempData["MsgType"] = "4";
                    }
                }
            }
            return View();
        }

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
                    var temp = new ResellerModel()
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
                cmd.CommandText = "UPDATE user SET user_id=@user, password=@password, name=@name, doa=@doa, contact_number=@contact, address=@address, email=@email, rout=@rout WHERE user_id=@prvUser AND type=3";
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
                cmd.CommandText = "UPDATE user SET user_id=@user, name=@name, doa=@doa, contact_number=@contact, address=@address, email=@email, rout=@rout WHERE user_id=@prvUser AND type=3";
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
            Console.WriteLine(hash);
            return hash;

        }
    }
}