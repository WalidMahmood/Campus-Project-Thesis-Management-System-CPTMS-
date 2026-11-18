using CPTMS.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace CPTMS.Models.ViewModels
{
    public class TeamSummaryViewModel
    {
        public int TeamId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string ProjectType { get; set; } = "Project";
        public string? Description { get; set; }
        public string LeaderName { get; set; } = "Unassigned";
        public int AcceptedMemberCount { get; set; }
        public int MinTeamSize { get; set; } = 2;
        public int MaxTeamSize { get; set; } = 4;
        public bool IsApproved { get; set; }
        public bool IsLocked { get; set; }
        public bool HasPendingRequestFromCurrentUser { get; set; }
        public bool HasPendingInviteForCurrentUser { get; set; }
        public bool CanRequestJoin => !IsLocked && AcceptedMemberCount < MaxTeamSize;
    }

    public class TeamsIndexViewModel
    {
        public bool IsStudent { get; set; }
        public bool IsCoordinator { get; set; }
        public ProjectTeam? MyActiveTeam { get; set; }
        public EligibilityCheckResult? MyTeamEligibility { get; set; }
        public List<TeamMembership> PendingInvitations { get; set; } = new();
        public List<TeamMembership> MyPendingRequests { get; set; } = new();
        public List<TeamSummaryViewModel> AllTeams { get; set; } = new();
        public string? SearchQuery { get; set; }
        public string? ProjectTypeFilter { get; set; }
    }

    public class CreateTeamViewModel
    {
        [Required(ErrorMessage = "Team name is required.")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Team name must be between 3 and 100 characters.")]
        [Display(Name = "Team Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Project type is required.")]
        [Display(Name = "Project Type")]
        public string ProjectType { get; set; } = "Project";

        [Required(ErrorMessage = "Department is required.")]
        [StringLength(100)]
        [Display(Name = "Department")]
        public string Department { get; set; } = "Computer Science and Engineering";

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
        [Display(Name = "Short Focus / Notes (Optional)")]
        public string? Description { get; set; }
    }

    public class TeamDetailsViewModel
    {
        public ProjectTeam Team { get; set; } = null!;
        public List<TeamMembership> AcceptedMembers { get; set; } = new();
        public List<TeamMembership> PendingJoinRequests { get; set; } = new();
        public List<TeamMembership> PendingInvitations { get; set; } = new();
        public EligibilityCheckResult EligibilityReport { get; set; } = new();
        public bool IsCurrentUserLeader { get; set; }
        public bool IsCurrentUserAcceptedMember { get; set; }
        public bool IsCurrentUserStudent { get; set; }
        public bool CurrentUserHasAnyTeam { get; set; }
        public bool CurrentUserHasPendingRequestHere { get; set; }
        public int? CurrentUserPendingInviteId { get; set; }
        public bool IsCoordinator { get; set; }
        public string? CurrentUserId { get; set; }
        public List<ApplicationUser> AvailableStudentsToInvite { get; set; } = new();
    }
}
