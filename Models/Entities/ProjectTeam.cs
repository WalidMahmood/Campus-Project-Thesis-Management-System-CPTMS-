using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CPTMS.Models.Entities
{
    public class ProjectTeam
    {
        [Key]
        public int TeamId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Department { get; set; } = "CSE";

        public string CreatedByStudentId { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsApproved { get; set; } = false;

        // Navigation properties
        public virtual ICollection<TeamMembership> Members { get; set; } = new List<TeamMembership>();
        public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
    }

    public class TeamMembership
    {
        [Key]
        public int MembershipId { get; set; }

        public int TeamId { get; set; }
        [ForeignKey("TeamId")]
        public virtual ProjectTeam? Team { get; set; }

        [Required]
        public string StudentId { get; set; } = string.Empty;
        [ForeignKey("StudentId")]
        public virtual ApplicationUser? Student { get; set; }

        public string MembershipRole { get; set; } = "Member"; // Leader, Member

        public string Status { get; set; } = "Pending"; // Pending, Accepted, Rejected

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
