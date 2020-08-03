using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SRIMAK.Models;
using SHA256 = SshNet.Security.Cryptography.SHA256;

namespace SRIMAK.Controllers
{
    public class LoginController : Controller
    {
        public LoginController(DBConnection DB)
        {
            DBConn = DB;
        }

        private DBConnection DBConn { get; }

        // GET: Login
        public ActionResult Index(int id)
        {
            if (id != 0)
            {
                ViewData["Message"] = "Invalid session";
            }
            else
            {
                HttpContext.Session.Clear();
            }

            return View("Create");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(IFormCollection collection)
        {
            try
            {
                var pass = ShaEncrypt(collection["Password"]);
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText =
                    @"SELECT user_id,name,email,doa,contact_number,address,type FROM user WHERE user_id=@userid AND password=@password";
                cmd.Parameters.AddWithValue("@userid", collection["UserName"]);
                cmd.Parameters.AddWithValue("@password", pass);

                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var model = new UserModel
                        {
                            UserId = reader.GetFieldValue<string>(0),
                            Name = reader.GetFieldValue<string>(1),
                            Email = reader.GetFieldValue<string>(2),
                            DOA = reader.GetFieldValue<DateTime>(3),
                            Contact = reader.GetFieldValue<string>(4),
                            Address = reader.GetFieldValue<string>(5),
                            Type = reader.GetFieldValue<int>(6)
                        };

                        HttpContext.Session.SetString("UID", model.UserId);
                        HttpContext.Session.SetString("Name", model.Name);

                        switch (model.Type)
                        {
                            case 1:
                                return RedirectToAction("Index", "ManagerDashboard");

                            case 2:
                                return RedirectToAction("Index", "ClerkDashboard");

                            case 3:
                                return RedirectToAction("Index", "ResellerDashboard");

                            default:
                                ViewData["Message"] = "Invalid login credentials!";
                                return View("Create");
                        }
                       
                    }
                }

                ViewData["Message"] = "Invalid login credentials!";
                return View("Create");
            }
            catch (Exception message)
            {
                ViewData["Message"] = message;
                Debug.WriteLine(message);
                return View();
            }
        }

        private static string ShaEncrypt(string input)
        {
            using var sha256 = HashAlgorithm.Create("sha256");
            // Send a sample text to hash.  
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            // Get the hashed string.  
            var hash = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            // Print the string.   
            //Console.WriteLine(hash);
            return hash;

        }

        public ActionResult Logout()
        {
            HttpContext.Session.Clear();
            return View("Create");
        }

    }
}