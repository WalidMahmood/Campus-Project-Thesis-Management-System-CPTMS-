using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CPTMS.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "User Role")]
        public string UserRole { get; set; } = "Student"; // Student, Supervisor, Evaluator, Coordinator, Administrator

        [Display(Name = "Department")]
        public string Department { get; set; } = "Computer Science and Engineering";

        // Student-specific fields
        [Display(Name = "Student ID")]
        public string? StudentIdNumber { get; set; }

        [Range(0.0, 4.0)]
        [Display(Name = "CGPA")]
        public double? CGPA { get; set; }

        public int? CompletedCredits { get; set; }

        // Faculty-specific fields
        [Display(Name = "Designation")]
        public string? Designation { get; set; }

        [Display(Name = "Supervision Capacity")]
        public int? MaxSupervisionCapacity { get; set; }

        [Display(Name = "Research Interests / Domain")]
        public string? ResearchInterests { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
