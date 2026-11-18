using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CPTMS.Models.Entities
{
    public class FacultyProfile
    {
        [Key]
        public int FacultyProfileId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Department")]
        public string Department { get; set; } = "Computer Science and Engineering";

        [Required]
        [StringLength(500)]
        [Display(Name = "Research Interests / Domains")]
        public string ResearchInterests { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Eligibility Conditions")]
        public string? EligibilityConditions { get; set; }

        [Range(0, 20)]
        [Display(Name = "Declared Maximum Supervision Load")]
        public int MaxSupervisionLoad { get; set; }

        [Range(0, 20)]
        [Display(Name = "Department-Approved Capacity Limit")]
        public int ApprovedCapacityLimit { get; set; }

        [Display(Name = "Current Supervision Load")]
        public int CurrentSupervisionLoad { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [NotMapped]
        public int RemainingSlots => Math.Max(0, MaxSupervisionLoad - CurrentSupervisionLoad);
    }
}
