using System.ComponentModel.DataAnnotations;

namespace CPTMS.Models.Entities
{
    public class EligibilityRule
    {
        [Key]
        public int RuleId { get; set; }

        [Required]
        [StringLength(120)]
        [Display(Name = "Rule Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        [Display(Name = "Project Type")]
        public string ProjectType { get; set; } = "Project"; // Project or Thesis

        [Required]
        [StringLength(100)]
        [Display(Name = "Department")]
        public string Department { get; set; } = "Computer Science and Engineering";

        [Range(1, 10)]
        [Display(Name = "Minimum Team Size")]
        public int MinTeamSize { get; set; } = 2;

        [Range(1, 10)]
        [Display(Name = "Maximum Team Size")]
        public int MaxTeamSize { get; set; } = 4;

        [Range(1, 16)]
        [Display(Name = "Minimum Semester")]
        public int MinSemester { get; set; } = 7;

        [Range(0.0, 4.0)]
        [Display(Name = "Minimum CGPA")]
        public double MinCgpa { get; set; } = 2.50;

        [Range(0, 200)]
        [Display(Name = "Minimum Completed Credits")]
        public int MinCompletedCredits { get; set; } = 75;

        [Display(Name = "Require Same Department")]
        public bool RequireSameDepartment { get; set; } = true;

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
