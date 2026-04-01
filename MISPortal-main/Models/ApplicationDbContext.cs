using Microsoft.AspNet.Identity.EntityFramework;
using System.Data.Entity;

namespace MisProject.Models
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext()
            : base("DefaultConnection")
        {
            // Disable model checking
            Database.SetInitializer<ApplicationDbContext>(null);
        }

        // Add Employee DbSet
        public DbSet<Employee> Employees { get; set; }

        // Add Department DbSet
        public DbSet<Department> Departments { get; set; }

        // Add Attendance DbSet
        public DbSet<Attendance> Attendances { get; set; }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }
}