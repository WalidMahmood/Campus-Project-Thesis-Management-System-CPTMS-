using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CPTMS.Models.Entities
{
    public class Project
    {
        [Key]
        public int ProjectId { get; set; }

        public int TeamId { get; set; }
        [ForeignKey("TeamId")]
        public virtual ProjectTeam? Team { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Type { get; set; } = "Project"; // Project or Thesis

        public string Status { get; set; } = "Draft"; // Draft, Submitted, UnderReview, Approved, InProgress, Completed, Archived

        public string? SupervisorId { get; set; }
        [ForeignKey("SupervisorId")]
        public virtual ApplicationUser? Supervisor { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Proposal> Proposals { get; set; } = new List<Proposal>();
        public virtual ICollection<Milestone> Milestones { get; set; } = new List<Milestone>();
    }

    public class Proposal
    {
        [Key]
        public int ProposalId { get; set; }

        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; }

        public int VersionNo { get; set; } = 1;

        [Required]
        public string ProblemStatement { get; set; } = string.Empty;

        [Required]
        public string Objectives { get; set; } = string.Empty;

        public string Methodology { get; set; } = string.Empty;

        public string ExpectedOutcomes { get; set; } = string.Empty;

        public string TechnologyStack { get; set; } = string.Empty;

        public string DecisionStatus { get; set; } = "Submitted"; // Submitted, RevisionsRequested, Approved, Rejected

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        public string? Feedback { get; set; }
    }

    public class Milestone
    {
        [Key]
        public int MilestoneId { get; set; }

        public int ProjectId { get; set; }
        [ForeignKey("ProjectId")]
        public virtual Project? Project { get; set; }

        [Required]
        [StringLength(150)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime DueDate { get; set; }

        public string Status { get; set; } = "Pending"; // Pending, InProgress, Submitted, Completed, Overdue
    }
}
