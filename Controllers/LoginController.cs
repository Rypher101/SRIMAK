using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using MySql.Data.MySqlClient;
using MySql.Data.Types;
using SRIMAK.Models;

namespace SRIMAK.Controllers
{
    public class LoginController : Controller
    {
        private DBConnection DBConn { get; set; }

        public LoginController(DBConnection DB)
        {
            this.DBConn = DB;
        }

        // GET: Login
        public ActionResult Index()
        {
            return View("Create");
        }

        // GET: Login/Details/5
        //public ActionResult Details(int id)
        //{
        //    return View();
        //}

        //// GET: Login/Create
        //public ActionResult Create()
        //{
        //    return View();
        //}

        // POST: Login/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(IFormCollection collection)
        {
            try
            {
                var cmd = this.DBConn.Connection.CreateCommand() as MySqlCommand;
                cmd.CommandText = @"SELECT user_id,name,email,doa,contact_number,address,type FROM user WHERE user_id=@userid AND password=@password";
                cmd.Parameters.AddWithValue("@userid", collection["UserName"]);
                cmd.Parameters.AddWithValue("@password",collection["Password"]);

                await using(var reader = await cmd.ExecuteReaderAsync())
                    while (await reader.ReadAsync())
                    {
                        var model = new UserModel()
                            {
                                UserId = reader.GetFieldValue<string>(0),
                                Name = reader.GetFieldValue<string>(1),
                                Email = reader.GetFieldValue<string>(2),
                                DOA = reader.GetFieldValue<DateTime>(3),
                                Contact = reader.GetFieldValue<string>(4),
                                Address = reader.GetFieldValue<string>(5),
                                Type = reader.GetFieldValue<int>(6)
                            };
                            ViewData["Message"] = "Login complete";
                            return View("Create");
                    }

                ViewData["Message"] = "Invalid login credentials!";
                return View("Create");
                
            }
            catch (Exception message)
            {
                System.Diagnostics.Debug.WriteLine(message);
                return View();
            }
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
        //        // TODO: Add update logic here

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
        //        // TODO: Add delete logic here

        //        return RedirectToAction(nameof(Index));
        //    }
        //    catch
        //    {
        //        return View();
        //    }
        //}
    }
}