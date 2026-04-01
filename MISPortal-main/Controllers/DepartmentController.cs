using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using MisProject.Models;

namespace MisProject.Controllers
{
    public class DepartmentController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: Department
        public ActionResult Index(string searchString)
        {
            try
            {
                var departments = db.Database.SqlQuery<Department>(
                    "SELECT * FROM Departments").ToList();

                if (!String.IsNullOrEmpty(searchString))
                {
                    departments = departments.Where(d =>
                        d.DepartmentName.Contains(searchString) ||
                        d.DepartmentCode.Contains(searchString)).ToList();
                }

                return View(departments.OrderBy(d => d.DepartmentName).ToList());
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error: " + ex.Message;
                return View(new List<Department>());
            }
        }

        // GET: Department/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }

            try
            {
                var department = db.Database.SqlQuery<Department>(
                    "SELECT * FROM Departments WHERE DepartmentId = @p0", id.Value).FirstOrDefault();

                if (department == null)
                {
                    return HttpNotFound();
                }
                return View(department);
            }
            catch
            {
                return RedirectToAction("Index");
            }
        }

        // GET: Department/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Department/Create - FIXED VERSION
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Department department)
        {
            System.Diagnostics.Debug.WriteLine($"Creating Department: {department.DepartmentCode}, {department.DepartmentName}");

            if (ModelState.IsValid)
            {
                try
                {
                    department.CreatedDate = DateTime.Now;
                    db.Departments.Add(department);
                    db.SaveChanges();

                    TempData["SuccessMessage"] = $"Department '{department.DepartmentName}' created successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    string errorMessage = GetExceptionMessage(ex);
                    ModelState.AddModelError("", "Save Error: " + errorMessage);
                    ViewBag.SaveError = errorMessage;
                    System.Diagnostics.Debug.WriteLine($"Save Error: {errorMessage}");
                }
            }
            else
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors);
                ViewBag.ValidationErrors = errors.Select(e => e.ErrorMessage).ToList();
            }

            return View(department);
        }

        // GET: Department/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }

            try
            {
                var department = db.Database.SqlQuery<Department>(
                    "SELECT * FROM Departments WHERE DepartmentId = @p0", id.Value).FirstOrDefault();

                if (department == null)
                {
                    return HttpNotFound();
                }
                return View(department);
            }
            catch
            {
                return RedirectToAction("Index");
            }
        }

        // POST: Department/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Department department)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    department.ModifiedDate = DateTime.Now;
                    db.Entry(department).State = EntityState.Modified;
                    db.SaveChanges();

                    TempData["SuccessMessage"] = $"Department '{department.DepartmentName}' updated successfully!";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Update Error: " + GetExceptionMessage(ex));
                }
            }
            return View(department);
        }

        // GET: Department/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index");
            }

            try
            {
                var department = db.Database.SqlQuery<Department>(
                    "SELECT * FROM Departments WHERE DepartmentId = @p0", id.Value).FirstOrDefault();

                if (department == null)
                {
                    return HttpNotFound();
                }

                // Check if department can be deleted
                ViewBag.CanDelete = CanDeleteDepartment(id.Value);
                return View(department);
            }
            catch
            {
                return RedirectToAction("Index");
            }
        }

        // POST: Department/Delete/5 - FIXED VERSION
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                // Step 1: Check if department can be deleted
                var checkResult = CanDeleteDepartment(id);
                if (!checkResult.CanDelete)
                {
                    TempData["ErrorMessage"] = checkResult.Message;
                    return RedirectToAction("Index");
                }

                // Step 2: Delete the department using SQL (to avoid EF tracking issues)
                int rowsAffected = db.Database.ExecuteSqlCommand(
                    "DELETE FROM Departments WHERE DepartmentId = @p0", id);

                if (rowsAffected > 0)
                {
                    TempData["SuccessMessage"] = "Department deleted successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Department not found!";
                }
            }
            catch (Exception ex)
            {
                string errorMsg = GetExceptionMessage(ex);
                System.Diagnostics.Debug.WriteLine($"Department Delete Error: {errorMsg}");

                if (errorMsg.Contains("foreign key") || errorMsg.Contains("FK_"))
                {
                    TempData["ErrorMessage"] = "Cannot delete department. It is being used by employees or users.";
                }
                else if (errorMsg.Contains("The DELETE statement conflicted"))
                {
                    TempData["ErrorMessage"] = "Cannot delete department. Related records exist in other tables.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error deleting department: " + errorMsg;
                }
            }

            return RedirectToAction("Index");
        }

        // Check if department can be deleted
        private DeleteCheckResult CanDeleteDepartment(int departmentId)
        {
            var result = new DeleteCheckResult { CanDelete = true };

            try
            {
                // Check Employees table
                var employeeCount = db.Database.SqlQuery<int>(
                    "SELECT COUNT(*) FROM Employees WHERE DepartmentId = @p0", departmentId).FirstOrDefault();

                // Check AspNetUsers table
                var userCount = db.Database.SqlQuery<int>(
                    "SELECT COUNT(*) FROM AspNetUsers WHERE DepartmentId = @p0", departmentId).FirstOrDefault();

                int totalReferences = employeeCount + userCount;

                if (totalReferences > 0)
                {
                    result.CanDelete = false;
                    result.Message = $"Cannot delete department!<br/>" +
                                   $"{employeeCount} employee(s) are assigned to this department.<br/>" +
                                   $"{userCount} user(s) are assigned to this department.<br/>" +
                                   "Please reassign these records first.";
                }
            }
            catch
            {
                result.CanDelete = false;
                result.Message = "Error checking department references.";
            }

            return result;
        }

        // Helper class for delete check result
        private class DeleteCheckResult
        {
            public bool CanDelete { get; set; }
            public string Message { get; set; }
        }

        // Force delete department (update references first)
        [HttpPost]
        public ActionResult ForceDelete(int id, string actionType)
        {
            try
            {
                // Step 1: Update references based on action type
                if (actionType == "null")
                {
                    // Set Employees DepartmentId to NULL
                    db.Database.ExecuteSqlCommand(
                        "UPDATE Employees SET DepartmentId = NULL WHERE DepartmentId = @p0", id);

                    // Set AspNetUsers DepartmentId to NULL
                    db.Database.ExecuteSqlCommand(
                        "UPDATE AspNetUsers SET DepartmentId = NULL WHERE DepartmentId = @p0", id);

                    TempData["InfoMessage"] = "Updated all references to NULL.";
                }
                else if (int.TryParse(actionType, out int newDeptId))
                {
                    // Move to another department
                    db.Database.ExecuteSqlCommand(
                        "UPDATE Employees SET DepartmentId = @p0 WHERE DepartmentId = @p1",
                        newDeptId, id);

                    db.Database.ExecuteSqlCommand(
                        "UPDATE AspNetUsers SET DepartmentId = @p0 WHERE DepartmentId = @p1",
                        newDeptId, id);

                    TempData["InfoMessage"] = $"Moved all references to Department ID: {newDeptId}";
                }

                // Step 2: Delete the department
                db.Database.ExecuteSqlCommand("DELETE FROM Departments WHERE DepartmentId = @p0", id);

                TempData["SuccessMessage"] = "Department deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Force delete error: " + GetExceptionMessage(ex);
            }

            return RedirectToAction("Index");
        }

        // Get available departments for moving references
        public ActionResult GetAvailableDepartments(int excludeId)
        {
            try
            {
                var departments = db.Database.SqlQuery<Department>(
                    "SELECT DepartmentId, DepartmentName FROM Departments WHERE DepartmentId != @p0 ORDER BY DepartmentName",
                    excludeId).ToList();

                return Json(departments, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(new List<Department>(), JsonRequestBehavior.AllowGet);
            }
        }

        // Helper method to get detailed error message
        private string GetExceptionMessage(Exception ex)
        {
            string message = ex.Message;
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
                message = ex.Message;
            }
            return message;
        }

        // TEST ACTION - Direct SQL save
        public ActionResult TestDirectSave()
        {
            try
            {
                string code = "TEST" + DateTime.Now.Second;
                string name = "Test Dept " + DateTime.Now.Second;

                string sql = $"INSERT INTO Departments (DepartmentCode, DepartmentName, Description, CreatedDate, IsActive) " +
                            $"VALUES ('{code}', '{name}', 'Test Description', GETDATE(), 1)";

                db.Database.ExecuteSqlCommand(sql);

                return Content($"Direct SQL Save Successful! Code: {code}, Name: {name}");
            }
            catch (Exception ex)
            {
                return Content($"SQL Error: {GetExceptionMessage(ex)}");
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