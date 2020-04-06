using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SRIMAK.Models;

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
                var cmd = DBConn.Connection.CreateCommand();
                cmd.CommandText =
                    @"SELECT user_id,name,email,doa,contact_number,address,type FROM user WHERE user_id=@userid AND password=@password";
                cmd.Parameters.AddWithValue("@userid", collection["UserName"]);
                cmd.Parameters.AddWithValue("@password", collection["Password"]);

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
                        return RedirectToAction("Index", "ManagerDashboard");
                    }
                }

                ViewData["Message"] = "Invalid login credentials!";
                return View("Create");
            }
            catch (Exception message)
            {
                Debug.WriteLine(message);
                return View();
            }
        }

        public ActionResult Logout()
        {
            HttpContext.Session.Clear();
            return View("Create");
        }

        // GET: Login/Edit/5
        //public ActionResult Edit(int id)
        //{
        //    return View();
        //}

        //// POST: Login/Edit/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Edit(int id, IFormCollection collection)
        //{
        //    try
        //    {
        //        

        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}

        //// GET: Login/Delete/5
        //public ActionResult Delete(int id)
        //{
        //    return View();
        //}

        //// POST: Login/Delete/5
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public ActionResult Delete(int id, IFormCollection collection)
        //{
        //    try
        //    {
        //        

        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}
    }
}