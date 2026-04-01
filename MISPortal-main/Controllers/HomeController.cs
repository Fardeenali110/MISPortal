using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using MisProject.Models;

namespace MisProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Home/Index - PROTECTED (Login Required)
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

                // Get today's attendance summary
                var today = DateTime.Today.ToString("yyyy-MM-dd");

                // Get present count (Status = 1)
                var presentCount = db.Database.SqlQuery<int>(
                    "SELECT COUNT(*) FROM Attendances WHERE Status = 1 AND AttendanceDate = @p0",
                    today).FirstOrDefault();

                // Get absent count (Status = 2)
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
                System.Diagnostics.Debug.WriteLine($"HomeController Error: {ex.Message}");

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

        // GET: Home/GetStats (for AJAX calls) - PROTECTED
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

        // GET: Home/GetRecentActivity - PROTECTED
        public ActionResult GetRecentActivity()
        {
            try
            {
                // Get recent attendance activities
                var recentActivities = db.Database.SqlQuery<string>(
                    @"SELECT TOP 5 
                        e.FirstName + ' ' + e.LastName + 
                        CASE 
                            WHEN a.Status = 1 THEN ' marked Present'
                            WHEN a.Status = 2 THEN ' marked Absent'
                            WHEN a.Status = 3 THEN ' applied for Leave'
                            ELSE ' attendance updated'
                        END + ' at ' + CONVERT(VARCHAR, a.CreatedDate, 108)
                      FROM Attendances a
                      INNER JOIN Employees e ON a.EmployeeId = e.EmployeeId
                      WHERE a.CreatedDate IS NOT NULL
                      ORDER BY a.CreatedDate DESC").ToList();

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

        // About Page - PROTECTED (remove if not needed)
        public ActionResult About()
        {
            ViewBag.Message = "About MIS Portal";
            return View();
        }

        // Contact Page - PROTECTED (remove if not needed)
        public ActionResult Contact()
        {
            ViewBag.Message = "Contact Us";
            return View();
        }

        // Test action for debugging - PROTECTED (remove if not needed)
        public ActionResult TestCounts()
        {
            try
            {
                var today = DateTime.Today.ToString("yyyy-MM-dd");

                string result = "<h2>Database Test Results</h2>";

                // Test employee count
                var empCount = db.Database.SqlQuery<int>("SELECT COUNT(*) FROM Employees WHERE IsActive = 1").FirstOrDefault();
                result += $"<p>✅ Active Employees: <strong>{empCount}</strong></p>";

                // Test department count
                var deptCount = db.Database.SqlQuery<int>("SELECT COUNT(*) FROM Departments WHERE IsActive = 1").FirstOrDefault();
                result += $"<p>✅ Active Departments: <strong>{deptCount}</strong></p>";

                // Test attendance
                var presentCount = db.Database.SqlQuery<int>("SELECT COUNT(*) FROM Attendances WHERE Status = 1 AND AttendanceDate = @p0", today).FirstOrDefault();
                var absentCount = db.Database.SqlQuery<int>("SELECT COUNT(*) FROM Attendances WHERE Status = 2 AND AttendanceDate = @p0", today).FirstOrDefault();
                var totalMarked = db.Database.SqlQuery<int>("SELECT COUNT(*) FROM Attendances WHERE AttendanceDate = @p0", today).FirstOrDefault();

                result += $"<p>✅ Today's Date: <strong>{today}</strong></p>";
                result += $"<p>✅ Present Today: <strong>{presentCount}</strong></p>";
                result += $"<p>✅ Absent Today: <strong>{absentCount}</strong></p>";
                result += $"<p>✅ Total Marked: <strong>{totalMarked}</strong></p>";
                result += $"<p>✅ Attendance Display: <strong>{presentCount}/{totalMarked}</strong></p>";

                // List all employees
                result += "<h3>All Active Employees:</h3>";
                var employees = db.Database.SqlQuery<EmployeeTest>("SELECT EmployeeId, FirstName, LastName FROM Employees WHERE IsActive = 1 ORDER BY FirstName").ToList();

                if (employees.Any())
                {
                    result += "<table border='1'><tr><th>ID</th><th>Name</th></tr>";
                    foreach (var emp in employees)
                    {
                        result += $"<tr><td>{emp.EmployeeId}</td><td>{emp.FirstName} {emp.LastName}</td></tr>";
                    }
                    result += "</table>";
                }
                else
                {
                    result += "<p style='color:red'>No employees found in database!</p>";
                }

                // Test attendance table
                result += "<h3>Today's Attendance Records:</h3>";
                try
                {
                    var attendanceRecords = db.Database.SqlQuery<AttendanceTest>(
                        "SELECT TOP 5 a.AttendanceId, e.FirstName + ' ' + e.LastName as EmployeeName, a.Status " +
                        "FROM Attendances a INNER JOIN Employees e ON a.EmployeeId = e.EmployeeId " +
                        "WHERE a.AttendanceDate = @p0 ORDER BY a.AttendanceId DESC", today).ToList();

                    if (attendanceRecords.Any())
                    {
                        result += "<table border='1'><tr><th>ID</th><th>Employee</th><th>Status</th></tr>";
                        foreach (var att in attendanceRecords)
                        {
                            string statusText = att.Status == 1 ? "Present" : att.Status == 2 ? "Absent" : "Leave";
                            result += $"<tr><td>{att.AttendanceId}</td><td>{att.EmployeeName}</td><td>{statusText}</td></tr>";
                        }
                        result += "</table>";
                    }
                    else
                    {
                        result += "<p>No attendance records for today</p>";
                    }
                }
                catch (Exception ex)
                {
                    result += $"<p style='color:red'>Error reading attendance: {ex.Message}</p>";
                }

                result += "<br><a href='/Home' class='btn btn-primary'>Back to Home</a>";

                return Content(result, "text/html");
            }
            catch (Exception ex)
            {
                return Content($"<h2 style='color:red'>ERROR: {ex.Message}</h2>", "text/html");
            }
        }

        // Helper classes
        private class EmployeeTest
        {
            public int EmployeeId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
        }

        private class AttendanceTest
        {
            public int AttendanceId { get; set; }
            public string EmployeeName { get; set; }
            public int Status { get; set; }
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