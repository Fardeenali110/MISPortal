using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MisProject.Models;

namespace MisProject.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Employee
        public ActionResult Index(string searchString)
        {
            try
            {
                // Get data with Department information
                var employeeData = db.Database.SqlQuery<EmployeeDto>(
                    @"SELECT 
                        e.EmployeeId, 
                        e.FirstName, 
                        e.LastName, 
                        e.Email, 
                        e.PhoneNumber, 
                        e.EmployeeCode,
                        e.DateOfBirth,
                        e.JoiningDate,
                        e.Designation,
                        e.Salary,
                        e.Address,
                        e.DepartmentId,
                        d.DepartmentName,
                        e.IsActive,
                        e.CreatedDate,
                        e.ModifiedDate
                      FROM Employees e
                      LEFT JOIN Departments d ON e.DepartmentId = d.DepartmentId").ToList();

                // Convert to Employee model
                var employees = new List<Employee>();
                foreach (var dto in employeeData)
                {
                    employees.Add(new Employee
                    {
                        EmployeeId = dto.EmployeeId,
                        FirstName = dto.FirstName,
                        LastName = dto.LastName,
                        Email = dto.Email,
                        PhoneNumber = dto.PhoneNumber,
                        EmployeeCode = dto.EmployeeCode,
                        DateOfBirth = dto.DateOfBirth,
                        JoiningDate = dto.JoiningDate,
                        Designation = dto.Designation,
                        Salary = dto.Salary,
                        Address = dto.Address,
                        DepartmentId = dto.DepartmentId,
                        Department = dto.DepartmentId.HasValue ? new Department
                        {
                            DepartmentId = dto.DepartmentId.Value,
                            DepartmentName = dto.DepartmentName
                        } : null,
                        IsActive = dto.IsActive,
                        CreatedDate = dto.CreatedDate,
                        ModifiedDate = dto.ModifiedDate
                    });
                }

                ViewBag.CurrentFilter = searchString;

                // Search
                if (!string.IsNullOrEmpty(searchString))
                {
                    employees = employees.Where(e =>
                        (e.FirstName != null && e.FirstName.Contains(searchString)) ||
                        (e.LastName != null && e.LastName.Contains(searchString)) ||
                        (e.Email != null && e.Email.Contains(searchString)) ||
                        (e.EmployeeCode != null && e.EmployeeCode.Contains(searchString))).ToList();
                }

                return View(employees);
            }
            catch (Exception)
            {
                ViewBag.Error = "Error loading employees";
                return View(new List<Employee>());
            }
        }

        // GET: Employee/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null) return RedirectToAction("Index");

            try
            {
                var dto = db.Database.SqlQuery<EmployeeDto>(
                    @"SELECT 
                        e.*,
                        d.DepartmentName 
                      FROM Employees e
                      LEFT JOIN Departments d ON e.DepartmentId = d.DepartmentId
                      WHERE e.EmployeeId = @p0", id.Value).FirstOrDefault();

                if (dto == null) return HttpNotFound();

                var employee = new Employee
                {
                    EmployeeId = dto.EmployeeId,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    EmployeeCode = dto.EmployeeCode,
                    DateOfBirth = dto.DateOfBirth,
                    JoiningDate = dto.JoiningDate,
                    Designation = dto.Designation,
                    Salary = dto.Salary,
                    Address = dto.Address,
                    DepartmentId = dto.DepartmentId,
                    Department = dto.DepartmentId.HasValue ? new Department
                    {
                        DepartmentId = dto.DepartmentId.Value,
                        DepartmentName = dto.DepartmentName
                    } : null,
                    IsActive = dto.IsActive,
                    CreatedDate = dto.CreatedDate,
                    ModifiedDate = dto.ModifiedDate
                };

                return View(employee);
            }
            catch
            {
                return RedirectToAction("Index");
            }
        }

        // GET: Employee/Create
        public ActionResult Create()
        {
            // Get departments for dropdown
            ViewBag.Departments = GetDepartmentList();
            return View();
        }

        // POST: Employee/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Employee employee)
        {
            // Get departments for dropdown (in case of error)
            ViewBag.Departments = GetDepartmentList();

            if (ModelState.IsValid)
            {
                try
                {
                    employee.CreatedDate = DateTime.Now;

                    string sql = @"INSERT INTO Employees 
                                  (FirstName, LastName, Email, PhoneNumber, EmployeeCode, 
                                   DateOfBirth, JoiningDate, Designation, Salary, Address, 
                                   DepartmentId, IsActive, CreatedDate) 
                                  VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10, @p11, @p12)";

                    db.Database.ExecuteSqlCommand(sql,
                        employee.FirstName,
                        employee.LastName,
                        employee.Email,
                        employee.PhoneNumber ?? "",
                        employee.EmployeeCode,
                        employee.DateOfBirth ?? (object)DBNull.Value,
                        employee.JoiningDate,
                        employee.Designation ?? "",
                        employee.Salary,
                        employee.Address ?? "",
                        employee.DepartmentId ?? (object)DBNull.Value,
                        employee.IsActive,
                        DateTime.Now);

                    TempData["SuccessMessage"] = $"Employee '{employee.FirstName} {employee.LastName}' created successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ViewBag.SaveError = "Error: " + ex.Message;
                }
            }
            return View(employee);
        }

        // GET: Employee/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null) return RedirectToAction("Index");

            try
            {
                var dto = db.Database.SqlQuery<EmployeeDto>(
                    @"SELECT * FROM Employees WHERE EmployeeId = @p0", id.Value).FirstOrDefault();

                if (dto == null) return HttpNotFound();

                var employee = new Employee
                {
                    EmployeeId = dto.EmployeeId,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    EmployeeCode = dto.EmployeeCode,
                    DateOfBirth = dto.DateOfBirth,
                    JoiningDate = dto.JoiningDate,
                    Designation = dto.Designation,
                    Salary = dto.Salary,
                    Address = dto.Address,
                    DepartmentId = dto.DepartmentId,
                    IsActive = dto.IsActive,
                    CreatedDate = dto.CreatedDate,
                    ModifiedDate = dto.ModifiedDate
                };

                // Get departments for dropdown
                ViewBag.Departments = GetDepartmentList();
                return View(employee);
            }
            catch
            {
                return RedirectToAction("Index");
            }
        }

        // POST: Employee/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Employee employee)
        {
            // Get departments for dropdown (in case of error)
            ViewBag.Departments = GetDepartmentList();

            if (ModelState.IsValid)
            {
                try
                {
                    employee.ModifiedDate = DateTime.Now;

                    string sql = @"UPDATE Employees SET 
                                   FirstName = @p0, 
                                   LastName = @p1, 
                                   Email = @p2, 
                                   PhoneNumber = @p3, 
                                   EmployeeCode = @p4,
                                   DateOfBirth = @p5,
                                   JoiningDate = @p6,
                                   Designation = @p7,
                                   Salary = @p8,
                                   Address = @p9,
                                   DepartmentId = @p10,
                                   IsActive = @p11,
                                   ModifiedDate = @p12
                                   WHERE EmployeeId = @p13";

                    db.Database.ExecuteSqlCommand(sql,
                        employee.FirstName,
                        employee.LastName,
                        employee.Email,
                        employee.PhoneNumber ?? "",
                        employee.EmployeeCode,
                        employee.DateOfBirth ?? (object)DBNull.Value,
                        employee.JoiningDate,
                        employee.Designation ?? "",
                        employee.Salary,
                        employee.Address ?? "",
                        employee.DepartmentId ?? (object)DBNull.Value,
                        employee.IsActive,
                        DateTime.Now,
                        employee.EmployeeId);

                    TempData["SuccessMessage"] = "Employee updated successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Error: " + ex.Message);
                }
            }
            return View(employee);
        }

        // GET: Employee/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null) return RedirectToAction("Index");

            try
            {
                var dto = db.Database.SqlQuery<EmployeeDto>(
                    @"SELECT * FROM Employees WHERE EmployeeId = @p0", id.Value).FirstOrDefault();

                if (dto == null) return HttpNotFound();

                var employee = new Employee
                {
                    EmployeeId = dto.EmployeeId,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Email = dto.Email,
                    PhoneNumber = dto.PhoneNumber,
                    EmployeeCode = dto.EmployeeCode,
                    DateOfBirth = dto.DateOfBirth,
                    JoiningDate = dto.JoiningDate,
                    Designation = dto.Designation,
                    Salary = dto.Salary,
                    Address = dto.Address,
                    DepartmentId = dto.DepartmentId,
                    IsActive = dto.IsActive,
                    CreatedDate = dto.CreatedDate,
                    ModifiedDate = dto.ModifiedDate
                };

                return View(employee);
            }
            catch
            {
                return RedirectToAction("Index");
            }
        }

        // POST: Employee/Delete/5 - FIXED VERSION
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                // Step 1: Check if employee exists
                var exists = db.Database.SqlQuery<int>(
                    "SELECT COUNT(*) FROM Employees WHERE EmployeeId = @p0", id).FirstOrDefault();

                if (exists == 0)
                {
                    TempData["ErrorMessage"] = "Employee not found!";
                    return RedirectToAction("Index");
                }

                // Step 2: First delete attendance records (if any)
                try
                {
                    int attendanceCount = db.Database.ExecuteSqlCommand(
                        "DELETE FROM Attendances WHERE EmployeeId = @p0", id);

                    System.Diagnostics.Debug.WriteLine($"Deleted {attendanceCount} attendance records for Employee ID: {id}");
                }
                catch (Exception attEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Attendance delete failed: {attEx.Message}");
                    // Continue anyway - maybe no attendance records
                }

                // Step 3: Delete the employee
                int rowsAffected = db.Database.ExecuteSqlCommand(
                    "DELETE FROM Employees WHERE EmployeeId = @p0", id);

                if (rowsAffected > 0)
                {
                    TempData["SuccessMessage"] = "Employee deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Employee not found or already deleted!";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Delete Error for Employee ID {id}: {ex.Message}");

                // Check error type
                if (ex.Message.Contains("foreign key") || ex.Message.Contains("FK__Attendanc"))
                {
                    TempData["ErrorMessage"] = "Cannot delete employee because it has related attendance records. Try deleting attendance records first.";
                }
                else if (ex.Message.Contains("The DELETE statement conflicted"))
                {
                    TempData["ErrorMessage"] = "Cannot delete employee. Related records exist in other tables.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error deleting employee: " + ex.Message;
                }
            }

            return RedirectToAction("Index");
        }

        // ========== DEBUG ACTIONS ==========

        // Debug: Check department dropdown
        public ActionResult DebugDepartmentDropdown()
        {
            try
            {
                var departments = GetDepartmentList();

                string result = "<h2>Department Dropdown Debug</h2>";
                result += $"<p>Total departments in dropdown: {departments.Count}</p>";

                if (departments.Any())
                {
                    result += "<table border='1'><tr><th>ID</th><th>Name</th><th>Code</th></tr>";
                    foreach (var dept in departments)
                    {
                        result += $"<tr><td>{dept.DepartmentId}</td><td>{dept.DepartmentName}</td><td>{dept.DepartmentCode}</td></tr>";
                    }
                    result += "</table>";
                }
                else
                {
                    result += "<p style='color:red'>No departments found!</p>";
                }

                return Content(result, "text/html");
            }
            catch (Exception ex)
            {
                return Content($"<h2 style='color:red'>ERROR: {ex.Message}</h2>", "text/html");
            }
        }

        // Test department assignment
        public ActionResult TestDepartmentAssignment(int employeeId, int departmentId)
        {
            try
            {
                db.Database.ExecuteSqlCommand(
                    "UPDATE Employees SET DepartmentId = @p0 WHERE EmployeeId = @p1",
                    departmentId, employeeId);

                return Content($"Assigned Department ID {departmentId} to Employee ID {employeeId}");
            }
            catch (Exception ex)
            {
                return Content($"Error: {ex.Message}");
            }
        }

        // ========== HELPER METHODS ==========

        // Helper: Get departments for dropdown - FIXED VERSION
        private List<Department> GetDepartmentList()
        {
            try
            {
                // Based on your database structure:
                // Column 1: DepartmentCode (contains Code like "IT", "CS-5")
                // Column 2: DepartmentName (contains Name like "Information Technology", "Computer Science")

                // Since your data is correct in database, use simple query
                return db.Database.SqlQuery<Department>(
                    "SELECT DepartmentId, DepartmentName, DepartmentCode " +
                    "FROM Departments WHERE IsActive = 1 " +
                    "ORDER BY DepartmentName").ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetDepartmentList Error: {ex.Message}");

                // Fallback with actual database IDs from your data
                return new List<Department>
                {
                    new Department { DepartmentId = 1, DepartmentName = "Information Technology", DepartmentCode = "IT" },
                    new Department { DepartmentId = 19, DepartmentName = "Computer Science", DepartmentCode = "CS-5" },
                    new Department { DepartmentId = 16, DepartmentName = "Software Engineering", DepartmentCode = "SE-1" },
                    new Department { DepartmentId = 18, DepartmentName = "Human Resources", DepartmentCode = "HR" }
                };
            }
        }

        // DTO Class for database mapping
        private class EmployeeDto
        {
            public int EmployeeId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string PhoneNumber { get; set; }
            public string EmployeeCode { get; set; }
            public DateTime? DateOfBirth { get; set; }
            public DateTime JoiningDate { get; set; }
            public string Designation { get; set; }
            public decimal Salary { get; set; }
            public string Address { get; set; }
            public int? DepartmentId { get; set; }
            public string DepartmentName { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedDate { get; set; }
            public DateTime? ModifiedDate { get; set; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}