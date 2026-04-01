using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using MisProject.Models;

namespace MisProject.Controllers
{
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Dashboard
        public ActionResult Index()
        {
            try
            {
                // Get total employees count
                var totalEmployees = db.Database.SqlQuery<int>(
                    "SELECT COUNT(*) FROM Employees WHERE IsActive = 1").FirstOrDefault();

                // Get total departments count
                var totalDepartments = db.Database.SqlQuery<int>(
                    "SELECT COUNT(*) FROM Departments WHERE IsActive = 1").FirstOrDefault();

                // Get today's attendance summary - FIXED
                var today = DateTime.Today.ToString("yyyy-MM-dd");

                // Get present count
                var presentCount = db.Database.SqlQuery<int>(
                    "SELECT COUNT(*) FROM Attendances WHERE Status = 1 AND AttendanceDate = @p0",
                    today).FirstOrDefault();

                // Get absent count
                var absentCount = db.Database.SqlQuery<int>(
                    "SELECT COUNT(*) FROM Attendances WHERE Status = 2 AND AttendanceDate = @p0",
                    today).FirstOrDefault();

                // Get total marked attendance today
                var totalMarkedToday = db.Database.SqlQuery<int>(
                    "SELECT COUNT(*) FROM Attendances WHERE AttendanceDate = @p0",
                    today).FirstOrDefault();

                // Format attendance string
                string attendanceText = $"{presentCount}/{totalMarkedToday}";
                if (totalMarkedToday == 0)
                {
                    attendanceText = "0/0";
                }

                // Prepare ViewBag
                ViewBag.TotalEmployees = totalEmployees;
                ViewBag.TotalDepartments = totalDepartments;
                ViewBag.PresentCount = presentCount;
                ViewBag.AbsentCount = absentCount;
                ViewBag.TotalMarkedToday = totalMarkedToday;
                ViewBag.TodaysAttendance = attendanceText;
                ViewBag.TodaysDate = DateTime.Now.ToString("dddd, MMMM dd, yyyy");
                ViewBag.CurrentTime = DateTime.Now.ToString("hh:mm tt");
                ViewBag.SystemStatus = "Online";

                return View();
            }
            catch (Exception ex)
            {
                // Log error
                System.Diagnostics.Debug.WriteLine($"Dashboard Error: {ex.Message}");

                // Return default values on error
                ViewBag.TotalEmployees = 0;
                ViewBag.TotalDepartments = 0;
                ViewBag.TodaysAttendance = "0/0";
                ViewBag.TodaysDate = DateTime.Now.ToString("dddd, MMMM dd, yyyy");
                ViewBag.SystemStatus = "Error";
                ViewBag.ErrorMessage = ex.Message;

                return View();
            }
        }

        // GET: Dashboard/GetStats (for AJAX calls)
        [HttpGet]
        public JsonResult GetStats()
        {
            try
            {
                var today = DateTime.Today.ToString("yyyy-MM-dd");

                var stats = new
                {
                    TotalEmployees = db.Database.SqlQuery<int>("SELECT COUNT(*) FROM Employees WHERE IsActive = 1").FirstOrDefault(),
                    TotalDepartments = db.Database.SqlQuery<int>("SELECT COUNT(*) FROM Departments WHERE IsActive = 1").FirstOrDefault(),
                    PresentToday = db.Database.SqlQuery<int>("SELECT COUNT(*) FROM Attendances WHERE Status = 1 AND AttendanceDate = @p0", today).FirstOrDefault(),
                    AbsentToday = db.Database.SqlQuery<int>("SELECT COUNT(*) FROM Attendances WHERE Status = 2 AND AttendanceDate = @p0", today).FirstOrDefault(),
                    TotalMarkedToday = db.Database.SqlQuery<int>("SELECT COUNT(*) FROM Attendances WHERE AttendanceDate = @p0", today).FirstOrDefault(),
                    CurrentTime = DateTime.Now.ToString("hh:mm tt"),
                    SystemStatus = "Online"
                };

                return Json(stats, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    TotalEmployees = 0,
                    TotalDepartments = 0,
                    PresentToday = 0,
                    AbsentToday = 0,
                    TotalMarkedToday = 0,
                    CurrentTime = DateTime.Now.ToString("hh:mm tt"),
                    SystemStatus = "Error",
                    Error = ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        // GET: Dashboard/GetRecentActivity
        public ActionResult GetRecentActivity()
        {
            try
            {
                // Get recent attendance activities
                var recentActivities = db.Database.SqlQuery<string>(
                    @"SELECT TOP 5 
                        'Employee ' + e.FirstName + ' ' + e.LastName + 
                        CASE 
                            WHEN a.Status = 1 THEN ' marked Present'
                            WHEN a.Status = 2 THEN ' marked Absent'
                            WHEN a.Status = 3 THEN ' applied for Leave'
                            ELSE ' attendance updated'
                        END + ' on ' + CONVERT(VARCHAR, a.ModifiedDate, 106)
                      FROM Attendances a
                      INNER JOIN Employees e ON a.EmployeeId = e.EmployeeId
                      WHERE a.ModifiedDate IS NOT NULL
                      ORDER BY a.ModifiedDate DESC").ToList();

                if (!recentActivities.Any())
                {
                    recentActivities = new List<string>
                    {
                        "System initialized successfully",
                        "No recent activities found"
                    };
                }

                return Json(recentActivities, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new List<string>
                {
                    "Error loading activities: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}