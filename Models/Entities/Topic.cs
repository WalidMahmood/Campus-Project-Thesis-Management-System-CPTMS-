using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CPTMS.Models.Entities
{
    public class Topic
    {
        [Key]
        public int TopicId { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "Topic Title")]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Domain")]
        public string Domain { get; set; } = string.Empty;

        [Required]
        [StringLength(300)]
        [Display(Name = "Required Skills")]
        public string RequiredSkills { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        [Display(Name = "Project Type")]
        public string ProjectType { get; set; } = "Project"; // Project or Thesis

        [Range(1, 10)]
        [Display(Name = "Team Capacity")]
        public int Capacity { get; set; } = 1;

        [Display(Name = "Currently Assigned Teams")]
        public int CurrentAssignedCount { get; set; } = 0;

        [Display(Name = "Open for Applications")]
        public bool IsAvailable { get; set; } = true;

        [Required]
        [Display(Name = "Faculty Owner")]
        public string FacultyOwnerId { get; set; } = string.Empty;

        [ForeignKey("FacultyOwnerId")]
        public virtual ApplicationUser? FacultyOwner { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // A topic can be manually opened but still has no free slots.
        [NotMapped]
        public bool IsOpenForApplications => IsAvailable && CurrentAssignedCount < Capacity;

        [NotMapped]
        public int RemainingSlots => Math.Max(0, Capacity - CurrentAssignedCount);
    }
}
