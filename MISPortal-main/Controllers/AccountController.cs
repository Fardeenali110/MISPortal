using MisProject.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace MisProject.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext db = new ApplicationDbContext();

        // GET: Account/Login
        [AllowAnonymous]
        public ActionResult Login()
        {
            // If already logged in, redirect to home
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public ActionResult Login(string username, string password, string returnUrl = "")
        {
            try
            {
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ViewBag.Error = "Username and password are required!";
                    return View();
                }

                // Find user
                var user = db.Database.SqlQuery<UserAccount>(
                    "SELECT UserId, Username, PasswordHash, Email, FullName, Role, IsActive, CreatedDate " +
                    "FROM Users WHERE Username = @p0 AND IsActive = 1", username).FirstOrDefault();

                if (user == null)
                {
                    ViewBag.Error = "Invalid username or password!";
                    return View();
                }

                // Verify password
                string hashedPassword = HashPassword(password);
                if (user.PasswordHash != hashedPassword)
                {
                    ViewBag.Error = "Invalid username or password!";
                    return View();
                }

                // Create authentication ticket
                FormsAuthentication.SetAuthCookie(username, false);

                // Update last login
                db.Database.ExecuteSqlCommand(
                    "UPDATE Users SET LastLoginDate = GETDATE() WHERE UserId = @p0",
                    user.UserId);

                // Redirect to return URL or home
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Login failed: " + ex.Message;
                return View();
            }
        }

        // GET: Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public ActionResult Register(string username, string password, string confirmPassword,
                                    string email, string fullName)
        {
            try
            {
                // Validation
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    ViewBag.Error = "Username and password are required!";
                    return View();
                }

                if (password != confirmPassword)
                {
                    ViewBag.Error = "Passwords do not match!";
                    return View();
                }

                // Check if username exists
                var existingUser = db.Database.SqlQuery<int>(
                    "SELECT COUNT(*) FROM Users WHERE Username = @p0", username).FirstOrDefault();

                if (existingUser > 0)
                {
                    ViewBag.Error = "Username already exists!";
                    return View();
                }

                // Check if email exists
                if (!string.IsNullOrEmpty(email))
                {
                    var existingEmail = db.Database.SqlQuery<int>(
                        "SELECT COUNT(*) FROM Users WHERE Email = @p0", email).FirstOrDefault();

                    if (existingEmail > 0)
                    {
                        ViewBag.Error = "Email already registered!";
                        return View();
                    }
                }

                // Create new user
                string passwordHash = HashPassword(password);
                string role = "User"; // Default role

                db.Database.ExecuteSqlCommand(
                    "INSERT INTO Users (Username, PasswordHash, Email, FullName, Role, IsActive, CreatedDate) " +
                    "VALUES (@p0, @p1, @p2, @p3, @p4, 1, GETDATE())",
                    username, passwordHash, email, fullName, role);

                TempData["SuccessMessage"] = "Registration successful! Please login.";

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Registration failed: " + ex.Message;
                return View();
            }
        }

        // GET: Account/Logout
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            Session.Clear();
            return RedirectToAction("Login");
        }

        // GET: Account/Profile
        [Authorize]
        public ActionResult Profile()
        {
            try
            {
                var username = User.Identity.Name;
                var user = db.Database.SqlQuery<UserAccount>(
                    "SELECT UserId, Username, Email, FullName, Role, IsActive, CreatedDate, LastLoginDate " +
                    "FROM Users WHERE Username = @p0", username).FirstOrDefault();

                if (user == null)
                {
                    FormsAuthentication.SignOut();
                    return RedirectToAction("Login");
                }

                return View(user);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error loading profile: " + ex.Message;
                return View();
            }
        }

        // POST: Account/ChangePassword
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            try
            {
                var username = User.Identity.Name;

                // Get current user
                var user = db.Database.SqlQuery<UserAccount>(
                    "SELECT PasswordHash FROM Users WHERE Username = @p0", username).FirstOrDefault();

                if (user == null)
                {
                    return Json(new { success = false, message = "User not found!" });
                }

                // Verify current password
                if (user.PasswordHash != HashPassword(currentPassword))
                {
                    return Json(new { success = false, message = "Current password is incorrect!" });
                }

                if (newPassword != confirmPassword)
                {
                    return Json(new { success = false, message = "New passwords do not match!" });
                }

                // Update password
                string newHash = HashPassword(newPassword);
                db.Database.ExecuteSqlCommand(
                    "UPDATE Users SET PasswordHash = @p0 WHERE Username = @p1",
                    newHash, username);

                return Json(new { success = true, message = "Password changed successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // GET: Account/ManageUsers (Admin only)
        [Authorize(Roles = "Admin")]
        public ActionResult ManageUsers()
        {
            try
            {
                var users = db.Database.SqlQuery<UserAccount>(
                    "SELECT UserId, Username, Email, FullName, Role, IsActive, CreatedDate, LastLoginDate " +
                    "FROM Users ORDER BY CreatedDate DESC").ToList();

                return View(users);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error loading users: " + ex.Message;
                return View(new List<UserAccount>());
            }
        }

        // POST: Account/ToggleUserStatus
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public JsonResult ToggleUserStatus(int userId)
        {
            try
            {
                var currentStatus = db.Database.SqlQuery<int>(
                    "SELECT IsActive FROM Users WHERE UserId = @p0", userId).FirstOrDefault();

                int newStatus = currentStatus == 1 ? 0 : 1;

                db.Database.ExecuteSqlCommand(
                    "UPDATE Users SET IsActive = @p0 WHERE UserId = @p1",
                    newStatus, userId);

                return Json(new
                {
                    success = true,
                    message = "User status updated!",
                    newStatus = newStatus
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // POST: Account/ChangeUserRole
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public JsonResult ChangeUserRole(int userId, string newRole)
        {
            try
            {
                db.Database.ExecuteSqlCommand(
                    "UPDATE Users SET Role = @p0 WHERE UserId = @p1",
                    newRole, userId);

                return Json(new { success = true, message = "User role updated!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        // Helper: Hash password
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        // Helper class for User
        public class UserAccount
        {
            public int UserId { get; set; }
            public string Username { get; set; }
            public string PasswordHash { get; set; }
            public string Email { get; set; }
            public string FullName { get; set; }
            public string Role { get; set; }
            public bool IsActive { get; set; }
            public DateTime CreatedDate { get; set; }
            public DateTime? LastLoginDate { get; set; }
        }

        // GET: Account/CreateAdmin (Run once to create admin user)
        [AllowAnonymous]
        public ActionResult CreateAdmin()
        {
            try
            {
                // Check if admin already exists
                var adminExists = db.Database.SqlQuery<int>(
                    "SELECT COUNT(*) FROM Users WHERE Username = 'admin'").FirstOrDefault();

                if (adminExists == 0)
                {
                    string adminHash = HashPassword("admin123");

                    db.Database.ExecuteSqlCommand(
                        "INSERT INTO Users (Username, PasswordHash, Email, FullName, Role, IsActive, CreatedDate) " +
                        "VALUES ('admin', @p0, 'admin@misportal.com', 'Administrator', 'Admin', 1, GETDATE())",
                        adminHash);

                    ViewBag.Message = "✅ Admin user created successfully!<br>Username: admin<br>Password: admin123";
                }
                else
                {
                    ViewBag.Message = "Admin user already exists!";
                }

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Error: " + ex.Message;
                return View();
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