using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using SRIMAK.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rotativa.AspNetCore;

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
        public async Task<ActionResult> Index()
        {
            var cmd = DBConn.Connection.CreateCommand();
            var rawList = new List<FinishedProductModel>();
            cmd.CommandText = "SELECT t2.rm_id, name, AVG(t2.qty) FROM (SELECT finished_product.rm_id, SUM(sales_product.qty) AS qty FROM (finished_product INNER JOIN sales_product ON finished_product.pro_id = sales_product.pro_id) INNER JOIN sales_order ON sales_order.so_id = sales_product.so_id WHERE sales_order.status = 3 AND (YEAR(sales_order.date) = YEAR(CURRENT_DATE - INTERVAL 1 MONTH) AND MONTH(sales_order.date) = MONTH(CURRENT_DATE - INTERVAL 1 MONTH)) GROUP BY finished_product.rm_id, DATE(date)) AS t2 INNER JOIN raw_materials ON t2.rm_id = raw_materials.rm_id GROUP BY rm_id";
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new FinishedProductModel()
                    {
                        rm_id = reader.GetFieldValue<int>(0),
                        Name = reader.GetFieldValue<string>(1),
                        avgQTY = reader.GetFieldValue<decimal>(2)
                    };

                    rawList.Add(temp);
                }
            }

            return View(rawList);
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
            else if (ViewBag.status == 3)
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

        public async Task<ActionResult> ViewSalesOrderDetailsResult(IFormCollection collection)
        {
            if (TempData["soID"] == null)
            {
                TempData["Message"] = "Couldn't find sales order. Please try again";
                TempData["MsgType"] = "4";
                return RedirectToAction(nameof(SalesOrder));
            }

            var proList = new List<FinishedProductModel>();
            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText = "SELECT pro_id FROM finished_product";
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new FinishedProductModel()
                    {
                        pro_id = reader.GetFieldValue<int>(0)
                    };
                    proList.Add(temp);
                }
            }

            cmd.CommandText = "UPDATE sales_order SET status = 2 WHERE so_id = @so";
            cmd.Parameters.AddWithValue("@so", TempData["soID"]);
            cmd.ExecuteNonQuery();
            cmd.Parameters.Clear();

            int recs = 0;
            foreach (var item in proList)
            {
                if (collection[item.pro_id.ToString()] != "")
                {
                    cmd.CommandText = "UPDATE sales_product SET new_qty = @qty WHERE pro_id = @pro AND so_id = @so";
                    cmd.Parameters.AddWithValue("@qty", int.Parse(collection[item.pro_id.ToString()]));
                    cmd.Parameters.AddWithValue("@pro", item.pro_id);
                    cmd.Parameters.AddWithValue("@so", TempData["soID"]);
                    recs += cmd.ExecuteNonQuery();
                    cmd.Parameters.Clear();
                }
            }

            if (recs > 0)
            {
                TempData["Message"] = "New order updated and accepted!";
                TempData["MsgType"] = "2";
            }
            else
            {
                TempData["Message"] = "Order accepted!";
                TempData["MsgType"] = "2";
            }

            return RedirectToAction(nameof(SalesOrder));
        }

        public async Task<ActionResult> ViewSalesOrderDistributorResult(IFormCollection collection)
        {
            if (TempData["soID"] == null)
            {
                TempData["Message"] = "Couldn't find sales order. Please try again";
                TempData["MsgType"] = "4";
                return RedirectToAction(nameof(SalesOrder));
            }

            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText = "UPDATE sales_order SET dis_id = @dis, status = 3 WHERE so_id = @soid";
            cmd.Parameters.AddWithValue("@dis", collection["disID"]);
            cmd.Parameters.AddWithValue("@soid", TempData["soID"]);
            var recs = cmd.ExecuteNonQuery();

            if (recs > 0)
            {
                string mailTable = "";
                cmd.CommandText =
                    "SELECT sales_product.pro_id, name, IF(sales_product.new_qty > 0 , sales_product.new_qty , sales_product.qty) FROM sales_product INNER JOIN finished_product ON sales_product.pro_id = finished_product.pro_id WHERE so_id = @soid";
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        mailTable += "<tr><td>" + reader.GetFieldValue<int>(0) + "</td><td>" + reader.GetFieldValue<string>(1) + "</td><td>" + reader.GetFieldValue<int>(2) + "</td></tr>";
                    }
                }

                cmd.CommandText = "SELECT due_date FROM sales_order WHERE so_id = @soid";
                DateTime dueDate =(DateTime) cmd.ExecuteScalar();

                var disEamail = "";
                var disName = "";

                cmd.CommandText = "SELECT name, email FROM distributor WHERE dis_id = @dis";
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        disName = reader.GetFieldValue<string>(0);
                        disEamail = reader.GetFieldValue<string>(1);
                    }
                }

                var reName = "";
                var reAddr = "";
                var reContact = "";

                cmd.CommandText =
                    "SELECT name, contact_number, address FROm user WHERE user_id = (SELECT user_id FROM sales_order WHERE so_id = @soid)";
                await using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        reName = reader.GetFieldValue<string>(0);
                        reAddr = reader.GetFieldValue<string>(2);
                        reContact = reader.GetFieldValue<string>(1);
                    }
                }

                try
                {
                    MimeMessage message = new MimeMessage();
                    MailboxAddress from = new MailboxAddress("SRIMAK", "srimak101@gmail.com");
                    message.From.Add(from);

                    MailboxAddress to = new MailboxAddress(disName, disEamail);
                    message.To.Add(to);

                    message.Subject = "New Sales Order " + TempData["soID"];

                    BodyBuilder bb = new BodyBuilder();
                    bb.HtmlBody = "<h3>Sales Order ID : " + TempData["soID"] + "</h3>" +
                                  "<br>" +
                                  "<h4>Reseller Name : " + reName + "</h4>" +
                                  "<h4>Reseller Contact No : " + reAddr + "</h4>" +
                                  "<h4>Reseller Address : " + reContact + "</h4>" +
                                  "<table> <thead> <tr> " +
                                  "<th>ID</th> <th>Product</th> <th>QTY</th>" +
                                  "</tr> </thead>" +
                                  "<tbody>" + mailTable + "</tbody>" +
                                  "</table>" +
                                  "<h4>Due Date : " + dueDate.ToString("D") + "</h4>";

                    message.Body = bb.ToMessageBody();

                    SmtpClient client = new SmtpClient();
                    client.Connect("smtp.gmail.com", 465, true);
                    client.Authenticate("srimak101@gmail.com", "sri123456789");
                    client.Send(message);
                    client.Disconnect(true);
                    client.Dispose();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    TempData["Message"] = "Distributor assigned but couldn't send mail to the distributor!";
                    TempData["MsgType"] = "4";
                    return RedirectToAction("ViewSalesOrder", new { id = TempData["soID"] });
                }

                TempData["Message"] = "Distributor assigned!";
                TempData["MsgType"] = "2";
                return RedirectToAction(nameof(SalesOrder));
            }
            else
            {
                TempData["Message"] = "Couldn't assign the distributor. Please try again";
                TempData["MsgType"] = "4";
                return RedirectToAction("ViewSalesOrder", new { id = TempData["soID"] });
            }
        }

        public async Task<ActionResult> CreateSalesOrderPDF(int id)
        {
            var output = new List<FinishedProductModel>();
            decimal totalCost = 0;
            int totalItems = 0;

            var cmd = DBConn.Connection.CreateCommand();
            cmd.CommandText =
                "SELECT sales_product.pro_id, name, IF(sales_product.new_qty > 0 , sales_product.new_qty , sales_product.qty), cost FROM sales_product INNER JOIN finished_product ON sales_product.pro_id = finished_product.pro_id WHERE so_id = @soid";
            cmd.Parameters.AddWithValue("@soid", id);

            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    var temp = new FinishedProductModel()
                    {
                        pro_id = reader.GetFieldValue<int>(0),
                        Name = reader.GetFieldValue<string>(1),
                        QTY = reader.GetFieldValue<int>(2)
                    };

                    totalItems += reader.GetFieldValue<int>(2);
                    totalCost += (reader.GetFieldValue<int>(2)*reader.GetFieldValue<decimal>(3));
                    output.Add(temp);
                }
            }

            cmd.CommandText = "SELECT name, contact_number, address, email FROM user WHERE user_id = (SELECT user_id FROM sales_order WHERE so_id = @soid)";
            await using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    ViewData["resellerName"] = reader.GetFieldValue<string>(0);
                    ViewData["resellerContact"] = reader.GetFieldValue<string>(1);
                    ViewData["resellerAddress"] = reader.GetFieldValue<string>(2);
                    ViewData["resellerEmail"] = reader.GetFieldValue<string>(3);
                }
            }

            ViewData["Title"] = "Sales Order : " + id;
            ViewData["so"] = id;
            ViewData["totalItems"] = totalItems;
            ViewData["totalCost"] = totalCost;

            return new ViewAsPdf("CreateSalesOrderPDF", output, ViewData);
        }

        public IActionResult DownloadSalesOrderPDF(int soid)
        {
            var report = new ViewAsPdf("CreateSalesOrderPDF", new{id = soid});
            return report;
        }

    }
}