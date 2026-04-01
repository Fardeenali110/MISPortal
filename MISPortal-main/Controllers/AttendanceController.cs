using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using System.IO;
using System.Text;
using MisProject.Models;
using ClosedXML.Excel;

namespace MisProject.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Attendance - Main attendance page
        public ActionResult Index()
        {
            try
            {
                // Get today's attendance with DTO
                var today = DateTime.Today.ToString("yyyy-MM-dd");

                var attendanceData = db.Database.SqlQuery<AttendanceDto>(
                    @"SELECT 
                        a.AttendanceId,
                        a.EmployeeId,
                        e.FirstName + ' ' + e.LastName as EmployeeName,
                        a.AttendanceDate,
                        a.Status,
                        a.CheckInTime,
                        a.CheckOutTime,
                        a.Remarks,
                        ISNULL(d.DepartmentName, 'Not Assigned') as DepartmentName
                      FROM Attendances a
                      INNER JOIN Employees e ON a.EmployeeId = e.EmployeeId
                      LEFT JOIN Departments d ON e.DepartmentId = d.DepartmentId
                      WHERE CAST(a.AttendanceDate AS DATE) = CAST(@p0 AS DATE)
                      ORDER BY a.AttendanceId DESC", today).ToList();

                return View(attendanceData);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error loading attendance: " + ex.Message;
                return View(new List<AttendanceDto>());
            }
        }

        // GET: Attendance/MarkAttendance - Mark attendance page
        public ActionResult MarkAttendance()
        {
            try
            {
                // Get all active employees for dropdown
                var employees = db.Database.SqlQuery<EmployeeDto>(
                    "SELECT EmployeeId, FirstName, LastName, EmployeeCode " +
                    "FROM Employees WHERE IsActive = 1 ORDER BY FirstName").ToList();

                ViewBag.Employees = employees;
                ViewBag.Today = DateTime.Today.ToString("yyyy-MM-dd");

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error: " + ex.Message;
                return View();
            }
        }

        // POST: Attendance/MarkAttendance - Save attendance
        [HttpPost]
        public ActionResult MarkAttendance(int employeeId, int status, string remarks = "")
        {
            try
            {
                var today = DateTime.Today.ToString("yyyy-MM-dd");

                // Check if attendance already exists for today
                var existing = db.Database.SqlQuery<int>(
                    "SELECT COUNT(*) FROM Attendances WHERE EmployeeId = @p0 AND CAST(AttendanceDate AS DATE) = CAST(@p1 AS DATE)",
                    employeeId, today).FirstOrDefault();

                if (existing > 0)
                {
                    // Update existing attendance
                    db.Database.ExecuteSqlCommand(
                        "UPDATE Attendances SET Status = @p0, Remarks = @p1, ModifiedDate = GETDATE() " +
                        "WHERE EmployeeId = @p2 AND CAST(AttendanceDate AS DATE) = CAST(@p3 AS DATE)",
                        status, remarks, employeeId, today);

                    TempData["Message"] = "Attendance updated successfully!";
                }
                else
                {
                    // Insert new attendance
                    db.Database.ExecuteSqlCommand(
                        "INSERT INTO Attendances (EmployeeId, AttendanceDate, Status, Remarks, CreatedDate) " +
                        "VALUES (@p0, @p1, @p2, @p3, GETDATE())",
                        employeeId, today, status, remarks);

                    TempData["Message"] = "Attendance marked successfully!";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
                return RedirectToAction("MarkAttendance");
            }
        }

        // GET: Attendance/EmployeeAttendance/{id} - View employee attendance history
        public ActionResult EmployeeAttendance(int? id)
        {
            try
            {
                if (!id.HasValue || id == 0)
                {
                    // If no id provided, show all employees to select from
                    var employees = db.Database.SqlQuery<EmployeeDto>(
                        "SELECT EmployeeId, FirstName, LastName, EmployeeCode " +
                        "FROM Employees WHERE IsActive = 1 ORDER BY FirstName").ToList();

                    return View("EmployeeList", employees);
                }

                var attendanceData = db.Database.SqlQuery<AttendanceDto>(
                    @"SELECT 
                        a.AttendanceId,
                        a.EmployeeId,
                        e.FirstName + ' ' + e.LastName as EmployeeName,
                        a.AttendanceDate,
                        a.Status,
                        a.CheckInTime,
                        a.CheckOutTime,
                        a.Remarks
                      FROM Attendances a
                      INNER JOIN Employees e ON a.EmployeeId = e.EmployeeId
                      WHERE a.EmployeeId = @p0
                      ORDER BY a.AttendanceDate DESC", id.Value).ToList();

                // Get employee details for view
                var employee = db.Database.SqlQuery<EmployeeDto>(
                    "SELECT EmployeeId, FirstName, LastName, EmployeeCode FROM Employees WHERE EmployeeId = @p0",
                    id.Value).FirstOrDefault();

                ViewBag.EmployeeId = id.Value;
                ViewBag.EmployeeName = employee != null ? $"{employee.FirstName} {employee.LastName}" : "Unknown Employee";
                ViewBag.EmployeeCode = employee?.EmployeeCode ?? "N/A";

                return View(attendanceData);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error: " + ex.Message;
                ViewBag.EmployeeId = id;
                return View(new List<AttendanceDto>());
            }
        }

        // GET: Attendance/Report - Attendance report with export
        public ActionResult Report(DateTime? fromDate, DateTime? toDate, string exportType = "")
        {
            try
            {
                if (!fromDate.HasValue) fromDate = DateTime.Today.AddDays(-30);
                if (!toDate.HasValue) toDate = DateTime.Today;

                var reportData = GetAttendanceReportData(fromDate.Value, toDate.Value);

                ViewBag.FromDate = fromDate.Value.ToString("yyyy-MM-dd");
                ViewBag.ToDate = toDate.Value.ToString("yyyy-MM-dd");
                ViewBag.TotalEmployees = reportData.Count;
                ViewBag.TotalPresent = reportData.Sum(r => r.PresentDays);
                ViewBag.TotalAbsent = reportData.Sum(r => r.AbsentDays);
                ViewBag.TotalLeave = reportData.Sum(r => r.LeaveDays);
                ViewBag.OverallPercentage = reportData.Any() ?
                    Math.Round(reportData.Average(r => r.AttendancePercentage), 2) : 0;

                // Handle export
                if (!string.IsNullOrEmpty(exportType))
                {
                    return ExportReport(reportData, exportType, fromDate.Value, toDate.Value);
                }

                return View(reportData);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error: " + ex.Message;
                return View(new List<AttendanceReportDto>());
            }
        }

        // POST: Attendance/Report - For form submission
        [HttpPost]
        public ActionResult Report(DateTime fromDate, DateTime toDate, string exportType = "")
        {
            return RedirectToAction("Report", new { fromDate, toDate, exportType });
        }

        // Helper: Get report data
        private List<AttendanceReportDto> GetAttendanceReportData(DateTime fromDate, DateTime toDate)
        {
            var rawData = db.Database.SqlQuery<AttendanceReportRawDto>(
                @"SELECT 
                    e.EmployeeId,
                    e.FirstName + ' ' + e.LastName as EmployeeName,
                    e.EmployeeCode,
                    ISNULL(d.DepartmentName, 'Not Assigned') as DepartmentName,
                    ISNULL(d.DepartmentCode, 'N/A') as DepartmentCode,
                    COUNT(CASE WHEN a.Status = 1 THEN 1 END) as PresentDays,
                    COUNT(CASE WHEN a.Status = 2 THEN 1 END) as AbsentDays,
                    COUNT(CASE WHEN a.Status = 3 THEN 1 END) as LeaveDays,
                    COUNT(*) as TotalDays,
                    ROUND((COUNT(CASE WHEN a.Status = 1 THEN 1 END) * 100.0 / NULLIF(COUNT(*), 0)), 2) as AttendancePercentage
                  FROM Employees e
                  LEFT JOIN Departments d ON e.DepartmentId = d.DepartmentId
                  LEFT JOIN Attendances a ON e.EmployeeId = a.EmployeeId 
                      AND CAST(a.AttendanceDate AS DATE) BETWEEN @p0 AND @p1
                  WHERE e.IsActive = 1
                  GROUP BY e.EmployeeId, e.FirstName, e.LastName, e.EmployeeCode, 
                           d.DepartmentName, d.DepartmentCode
                  ORDER BY e.FirstName",
                fromDate.ToString("yyyy-MM-dd"),
                toDate.ToString("yyyy-MM-dd")).ToList();

            // Convert to final DTO
            var result = new List<AttendanceReportDto>();
            foreach (var item in rawData)
            {
                result.Add(new AttendanceReportDto
                {
                    EmployeeId = item.EmployeeId,
                    EmployeeName = item.EmployeeName,
                    EmployeeCode = item.EmployeeCode,
                    DepartmentName = item.DepartmentName,
                    DepartmentCode = item.DepartmentCode,
                    PresentDays = item.PresentDays,
                    AbsentDays = item.AbsentDays,
                    LeaveDays = item.LeaveDays,
                    TotalDays = item.TotalDays,
                    AttendancePercentage = item.AttendancePercentage
                });
            }

            return result;
        }

        // Export to Excel or CSV
        private ActionResult ExportReport(List<AttendanceReportDto> data, string exportType, DateTime fromDate, DateTime toDate)
        {
            string fileName = $"Attendance_Report_{fromDate:yyyyMMdd}_to_{toDate:yyyyMMdd}";

            if (exportType.ToLower() == "excel")
            {
                return ExportToExcel(data, fileName + ".xlsx", fromDate, toDate);
            }
            else if (exportType.ToLower() == "csv")
            {
                return ExportToCSV(data, fileName + ".csv", fromDate, toDate);
            }

            return RedirectToAction("Report", new { fromDate, toDate });
        }

        // Export to Excel using ClosedXML
        private ActionResult ExportToExcel(List<AttendanceReportDto> data, string fileName, DateTime fromDate, DateTime toDate)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Attendance Report");

                // Title
                worksheet.Cell(1, 1).Value = "ATTENDANCE REPORT";
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Range(1, 1, 1, 10).Merge();

                // Period
                worksheet.Cell(2, 1).Value = $"Period: {fromDate:dd/MM/yyyy} to {toDate:dd/MM/yyyy}";
                worksheet.Cell(2, 1).Style.Font.FontSize = 12;
                worksheet.Range(2, 1, 2, 10).Merge();

                worksheet.Cell(3, 1).Value = $"Generated On: {DateTime.Now:dd/MM/yyyy hh:mm tt}";
                worksheet.Range(3, 1, 3, 10).Merge();

                // Headers
                var headerRow = worksheet.Row(5);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

                worksheet.Cell(5, 1).Value = "S.No";
                worksheet.Cell(5, 2).Value = "Employee ID";
                worksheet.Cell(5, 3).Value = "Employee Code";
                worksheet.Cell(5, 4).Value = "Employee Name";
                worksheet.Cell(5, 5).Value = "Department";
                worksheet.Cell(5, 6).Value = "Present";
                worksheet.Cell(5, 7).Value = "Absent";
                worksheet.Cell(5, 8).Value = "Leave";
                worksheet.Cell(5, 9).Value = "Total Days";
                worksheet.Cell(5, 10).Value = "Percentage";

                // Data
                int row = 6;
                int serial = 1;

                foreach (var item in data)
                {
                    worksheet.Cell(row, 1).Value = serial++;
                    worksheet.Cell(row, 2).Value = item.EmployeeId;
                    worksheet.Cell(row, 3).Value = item.EmployeeCode;
                    worksheet.Cell(row, 4).Value = item.EmployeeName;
                    worksheet.Cell(row, 5).Value = item.DepartmentName;
                    worksheet.Cell(row, 6).Value = item.PresentDays;
                    worksheet.Cell(row, 7).Value = item.AbsentDays;
                    worksheet.Cell(row, 8).Value = item.LeaveDays;
                    worksheet.Cell(row, 9).Value = item.TotalDays;
                    worksheet.Cell(row, 10).Value = item.AttendancePercentage + "%";

                    // Format percentage cell
                    if (item.AttendancePercentage >= 90)
                        worksheet.Cell(row, 10).Style.Fill.BackgroundColor = XLColor.LightGreen;
                    else if (item.AttendancePercentage >= 75)
                        worksheet.Cell(row, 10).Style.Fill.BackgroundColor = XLColor.LightYellow;
                    else
                        worksheet.Cell(row, 10).Style.Fill.BackgroundColor = XLColor.LightSalmon;

                    row++;
                }

                // Summary row
                if (data.Any())
                {
                    worksheet.Cell(row, 1).Value = "TOTAL";
                    worksheet.Cell(row, 1).Style.Font.Bold = true;
                    worksheet.Range(row, 1, row, 5).Merge();
                    worksheet.Cell(row, 6).Value = data.Sum(d => d.PresentDays);
                    worksheet.Cell(row, 7).Value = data.Sum(d => d.AbsentDays);
                    worksheet.Cell(row, 8).Value = data.Sum(d => d.LeaveDays);
                    worksheet.Cell(row, 9).Value = data.Sum(d => d.TotalDays);
                    worksheet.Cell(row, 10).Value = Math.Round(data.Average(d => d.AttendancePercentage), 2) + "%";
                }

                // Format cells
                worksheet.Columns().AdjustToContents();

                // Create memory stream
                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    stream.Position = 0;

                    return File(stream.ToArray(),
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName);
                }
            }
        }

        // Export to CSV (Simple, no PDF dependency)
        private ActionResult ExportToCSV(List<AttendanceReportDto> data, string fileName, DateTime fromDate, DateTime toDate)
        {
            var sb = new StringBuilder();

            // Header
            sb.AppendLine("ATTENDANCE REPORT");
            sb.AppendLine($"Period: {fromDate:dd/MM/yyyy} to {toDate:dd/MM/yyyy}");
            sb.AppendLine($"Generated On: {DateTime.Now:dd/MM/yyyy hh:mm tt}");
            sb.AppendLine();

            // Column headers
            sb.AppendLine("S.No,Employee ID,Employee Code,Employee Name,Department,Present,Absent,Leave,Total Days,Percentage,Status");

            // Data rows
            int serial = 1;
            foreach (var item in data)
            {
                string status = item.AttendancePercentage >= 90 ? "Excellent" :
                              item.AttendancePercentage >= 75 ? "Good" : "Poor";

                sb.AppendLine($"{serial++},{item.EmployeeId},{item.EmployeeCode}," +
                             $"\"{item.EmployeeName}\",\"{item.DepartmentName}\"," +
                             $"{item.PresentDays},{item.AbsentDays},{item.LeaveDays}," +
                             $"{item.TotalDays},{item.AttendancePercentage:F2}%,{status}");
            }

            // Summary row
            if (data.Any())
            {
                double avgPercentage = Math.Round(data.Average(d => d.AttendancePercentage), 2);
                sb.AppendLine();
                sb.AppendLine($"TOTAL,,,,{data.Sum(d => d.PresentDays)},{data.Sum(d => d.AbsentDays)}," +
                             $"{data.Sum(d => d.LeaveDays)},{data.Sum(d => d.TotalDays)},{avgPercentage}%,AVERAGE");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", fileName);
        }

        // ========== HELPER DTO CLASSES ==========

        // DTO for attendance display
        public class AttendanceDto
        {
            public int AttendanceId { get; set; }
            public int EmployeeId { get; set; }
            public string EmployeeName { get; set; }
            public DateTime AttendanceDate { get; set; }
            public int Status { get; set; }
            public TimeSpan? CheckInTime { get; set; }
            public TimeSpan? CheckOutTime { get; set; }
            public string Remarks { get; set; }
            public string DepartmentName { get; set; }
        }

        // DTO for employee dropdown
        public class EmployeeDto
        {
            public int EmployeeId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string EmployeeCode { get; set; }
        }

        // RAW DTO for SQL query results
        private class AttendanceReportRawDto
        {
            public int EmployeeId { get; set; }
            public string EmployeeName { get; set; }
            public string EmployeeCode { get; set; }
            public string DepartmentName { get; set; }
            public string DepartmentCode { get; set; }
            public int PresentDays { get; set; }
            public int AbsentDays { get; set; }
            public int LeaveDays { get; set; }
            public int TotalDays { get; set; }
            public double AttendancePercentage { get; set; }
        }

        // Public DTO for attendance report
        public class AttendanceReportDto
        {
            public int EmployeeId { get; set; }
            public string EmployeeName { get; set; }
            public string EmployeeCode { get; set; }
            public string DepartmentName { get; set; }
            public string DepartmentCode { get; set; }
            public int PresentDays { get; set; }
            public int AbsentDays { get; set; }
            public int LeaveDays { get; set; }
            public int TotalDays { get; set; }
            public double AttendancePercentage { get; set; }
        }

        // ========== UTILITY METHODS ==========

        // Quick add today's attendance for all employees
        public ActionResult AddToday()
        {
            try
            {
                var employees = db.Database.SqlQuery<int>(
                    "SELECT EmployeeId FROM Employees WHERE IsActive = 1").ToList();

                int added = 0;
                string today = DateTime.Today.ToString("yyyy-MM-dd");

                foreach (var empId in employees)
                {
                    var exists = db.Database.SqlQuery<int>(
                        "SELECT COUNT(*) FROM Attendances WHERE EmployeeId = @p0 AND CAST(AttendanceDate AS DATE) = CAST(@p1 AS DATE)",
                        empId, today).FirstOrDefault();

                    if (exists == 0)
                    {
                        db.Database.ExecuteSqlCommand(
                            "INSERT INTO Attendances (EmployeeId, AttendanceDate, Status, CreatedDate) " +
                            "VALUES (@p0, @p1, 1, GETDATE())",
                            empId, today);

                        added++;
                    }
                }

                TempData["Message"] = $"Added today's attendance for {added} employees!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // Add sample attendance with random status
        public ActionResult AddSampleAttendance()
        {
            try
            {
                var employees = db.Database.SqlQuery<int>(
                    "SELECT EmployeeId FROM Employees WHERE IsActive = 1").ToList();

                int added = 0;
                string today = DateTime.Today.ToString("yyyy-MM-dd");
                Random rnd = new Random();

                foreach (var empId in employees)
                {
                    var exists = db.Database.SqlQuery<int>(
                        "SELECT COUNT(*) FROM Attendances WHERE EmployeeId = @p0 AND CAST(AttendanceDate AS DATE) = CAST(@p1 AS DATE)",
                        empId, today).FirstOrDefault();

                    if (exists == 0)
                    {
                        // Random status: 80% Present, 15% Absent, 5% Leave
                        int random = rnd.Next(1, 101);
                        int status = 1; // Default Present

                        if (random > 80 && random <= 95)
                            status = 2; // Absent
                        else if (random > 95)
                            status = 3; // Leave

                        db.Database.ExecuteSqlCommand(
                            "INSERT INTO Attendances (EmployeeId, AttendanceDate, Status, Remarks, CreatedDate) " +
                            "VALUES (@p0, @p1, @p2, @p3, GETDATE())",
                            empId, today, status,
                            status == 1 ? "Auto-marked Present" :
                            status == 2 ? "Auto-marked Absent" : "Auto-marked Leave");

                        added++;
                    }
                }

                TempData["Message"] = $"Added sample attendance for {added} employees!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        // Employee List View for selection (when no id provided)
        public ActionResult EmployeeList()
        {
            try
            {
                var employees = db.Database.SqlQuery<EmployeeDto>(
                    "SELECT EmployeeId, FirstName, LastName, EmployeeCode " +
                    "FROM Employees WHERE IsActive = 1 ORDER BY FirstName").ToList();

                return View(employees);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error: " + ex.Message;
                return View(new List<EmployeeDto>());
            }
        }

        // Clear today's attendance
        public ActionResult ClearToday()
        {
            try
            {
                string today = DateTime.Today.ToString("yyyy-MM-dd");

                int deleted = db.Database.ExecuteSqlCommand(
                    "DELETE FROM Attendances WHERE CAST(AttendanceDate AS DATE) = CAST(@p0 AS DATE)",
                    today);

                TempData["Message"] = $"Cleared attendance for today. {deleted} records deleted.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error: " + ex.Message;
                return RedirectToAction("Index");
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