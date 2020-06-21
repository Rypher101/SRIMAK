using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SRIMAK.Models;

namespace SRIMAK.Controllers
{
    public class ClerkDashboardController : Controller
    {
        public ClerkDashboardController(DBConnection DB)
        {
            DBConn = DB;
        }

        private DBConnection DBConn { get; }

        // GET: ClerkDashboard
        public ActionResult Index()
        {
            return View();
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

        public async Task<ActionResult> Consumption()
        {
            var outputMonth = new List<DailyProductionModel>();
            var output6Month = new List<DailyProductionModel>();
            var output12Month = new List<DailyProductionModel>();

            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT date, daily_production.rm_id,name, prod, wast FROM daily_production INNER JOIN raw_materials ON daily_production.rm_id=raw_materials.rm_id WHERE YEAR(date) = YEAR(CURRENT_DATE - INTERVAL 1 MONTH) AND MONTH(date) = MONTH(CURRENT_DATE - INTERVAL 1 MONTH)";
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
            
            cmd.CommandText =
                "SELECT daily_production.rm_id, name, SUM(prod), SUM(wast) FROM daily_production INNER JOIN raw_materials ON daily_production.rm_id=raw_materials.rm_id WHERE date BETWEEN DATE_SUB(CURRENT_DATE(), INTERVAL 6 MONTH) AND DATE_SUB(CURRENT_DATE(), INTERVAL 1 MONTH) GROUP BY daily_production.rm_id";
            await using (var reader2 = await cmd.ExecuteReaderAsync())
            {
                while (await reader2.ReadAsync())
                {
                    var temp = new DailyProductionModel
                    {
                        rmID = reader2.GetFieldValue<int>(0),
                        Name = reader2.GetFieldValue<string>(1),
                        Production = Decimal.ToInt32(reader2.GetFieldValue<decimal>(2)),
                        Wastage = Decimal.ToInt32(reader2.GetFieldValue<decimal>(3))
                    };

                    output6Month.Add(temp);
                }
            }
            ViewBag.Month6 = output6Month;

            cmd.CommandText =
                "SELECT daily_production.rm_id, name, SUM(prod), SUM(wast) FROM daily_production INNER JOIN raw_materials ON daily_production.rm_id=raw_materials.rm_id WHERE date BETWEEN DATE_SUB(CURRENT_DATE(), INTERVAL 1 YEAR) AND DATE_SUB(CURRENT_DATE(), INTERVAL 1 MONTH) GROUP BY daily_production.rm_id";
            await using (var reader3 = await cmd.ExecuteReaderAsync())
            {
                while (await reader3.ReadAsync())
                {
                    var temp = new DailyProductionModel
                    {
                        rmID = reader3.GetFieldValue<int>(0),
                        Name = reader3.GetFieldValue<string>(1),
                        Production = Decimal.ToInt32(reader3.GetFieldValue<decimal>(2)),
                        Wastage = Decimal.ToInt32(reader3.GetFieldValue<decimal>(3))
                    };

                    output12Month.Add(temp);
                }
            }
            ViewBag.Month12 = output12Month;

            return View(outputMonth);
        }

        public async Task<ActionResult> CreateNewConsumption()
        {
            var output = await GetRawMaterials();
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText = "SELECT rm_id FROM daily_production WHERE DATE(date)=CURRENT_DATE()";
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    TempData["Message"] = "Today's consumption report has been already submitted!";
                    TempData["MsgType"] = "4";
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(output);
        }

        public async Task<ActionResult> CreateNewConsumptionResult(IFormCollection collection)
        {
            var raw = await GetRawMaterials();
            var cmd = DBConn.Connection.CreateCommand();

            foreach (var item in raw)
            {
                var prod = int.Parse(collection[item.Id + " prod"]);
                var wast = int.Parse(collection[item.Id + " wast"]);
                if (prod > 0 || wast > 0)
                {
                    cmd.Parameters.Clear();
                    cmd.CommandText = "INSERT INTO daily_production(rm_id,prod,wast) VALUES (@rmid,@prod,@wast)";
                    cmd.Parameters.AddWithValue("@rmid", item.Id);
                    cmd.Parameters.AddWithValue("@prod", prod);
                    cmd.Parameters.AddWithValue("@wast", wast);
                    cmd.ExecuteNonQuery();

                    cmd.Parameters.Clear();
                    cmd.CommandText = "UPDATE raw_materials SET qty=qty-@cons WHERE rm_id=@rmid";
                    cmd.Parameters.AddWithValue("@cons", prod + wast);
                    cmd.Parameters.AddWithValue("@rmid", item.Id);
                    cmd.ExecuteNonQuery();

                    if (prod > 0)
                    {
                        cmd.Parameters.Clear();
                        cmd.CommandText = "UPDATE finished_product SET qty=qty+@prod WHERE rm_id=@rmid";
                        cmd.Parameters.AddWithValue("@prod", prod);
                        cmd.Parameters.AddWithValue("@rmid", item.Id);
                        cmd.ExecuteNonQuery();
                    }
                }
            }

            TempData["Message"] = "Consumption report submited!";
            TempData["MsgType"] = "2";
            return RedirectToAction(nameof(CreateNewConsumption));
        }
    }
}