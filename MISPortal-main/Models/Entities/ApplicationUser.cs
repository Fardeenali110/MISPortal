using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MisProject.Models.Entities
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; }

        [StringLength(20)]
        public string EmployeeId { get; set; }

        public int? DepartmentId { get; set; }

        [StringLength(20)]
        public string MobileNumber { get; set; }

        public DateTime? DateOfBirth { get; set; }

        [StringLength(500)]
        public string Address { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }

        public bool IsActive { get; set; } = true;

        [NotMapped]
        public string FullName => $"{FirstName} {LastName}";

        // Navigation property
        public virtual Department Department { get; set; }
    }
}