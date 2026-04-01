using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MisProject.Models
{
    public class Attendance
    {
        [Key]
        public int AttendanceId { get; set; }

        [Required]
        [Display(Name = "Employee")]
        public int EmployeeId { get; set; }

        [Required]
        [Display(Name = "Date")]
        [DataType(DataType.Date)]
        public DateTime AttendanceDate { get; set; } = DateTime.Now.Date;

        [Required]
        [Display(Name = "Status")]
        public int Status { get; set; } = 1; // 1=Present, 2=Absent, 3=Late, 4=HalfDay

        [Display(Name = "Check-In")]
        [DataType(DataType.Time)]
        public TimeSpan? CheckInTime { get; set; }

        [Display(Name = "Check-Out")]
        [DataType(DataType.Time)]
        public TimeSpan? CheckOutTime { get; set; }

        [Display(Name = "Total Hours")]
        public decimal? TotalHours { get; set; }

        [StringLength(500)]
        [Display(Name = "Remarks")]
        public string Remarks { get; set; }

        [Display(Name = "Created Date")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "Modified Date")]
        public DateTime? ModifiedDate { get; set; }

        [ForeignKey("EmployeeId")]
        public virtual Employee Employee { get; set; }
    }
}