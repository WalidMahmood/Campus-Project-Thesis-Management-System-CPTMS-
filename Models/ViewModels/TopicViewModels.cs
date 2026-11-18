using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CPTMS.Models.ViewModels
{
    public class TopicSearchViewModel
    {
        [Display(Name = "Keyword")]
        public string? SearchQuery { get; set; }

        [Display(Name = "Domain")]
        public string? SelectedDomain { get; set; }

        [Display(Name = "Skill")]
        public string? SkillFilter { get; set; }

        [Display(Name = "Project Type")]
        public string? SelectedProjectType { get; set; }

        [Display(Name = "Only available topics")]
        public bool OnlyAvailable { get; set; }

        public List<string> AvailableDomains { get; set; } = new();
        public List<string> ProjectTypes { get; set; } = new();
        public List<TopicCardViewModel> Results { get; set; } = new();
    }

    public class TopicCardViewModel
    {
        public int TopicId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string RequiredSkills { get; set; } = string.Empty;
        public string ProjectType { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int CurrentAssignedCount { get; set; }
        public int RemainingSlots { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsOpenForApplications { get; set; }
        public string FacultyOwnerName { get; set; } = string.Empty;
        public string FacultyOwnerEmail { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class TopicDetailsViewModel
    {
        public int TopicId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public string RequiredSkills { get; set; } = string.Empty;
        public string ProjectType { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public int CurrentAssignedCount { get; set; }
        public int RemainingSlots { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsOpenForApplications { get; set; }
        public string FacultyOwnerId { get; set; } = string.Empty;
        public string FacultyOwnerName { get; set; } = string.Empty;
        public string FacultyOwnerEmail { get; set; } = string.Empty;
        public string? FacultyOwnerDesignation { get; set; }
        public string? FacultyOwnerDepartment { get; set; }
        public string? FacultyOwnerResearchInterests { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TopicFormViewModel
    {
        public int TopicId { get; set; }

        [Required(ErrorMessage = "Topic title is required.")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters.")]
        [Display(Name = "Topic Title")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Domain is required.")]
        [StringLength(100)]
        [Display(Name = "Domain")]
        public string Domain { get; set; } = string.Empty;

        [Required(ErrorMessage = "Required skills are required.")]
        [StringLength(300, ErrorMessage = "Skills cannot exceed 300 characters.")]
        [Display(Name = "Required Skills (comma separated)")]
        public string RequiredSkills { get; set; } = string.Empty;

        [Required(ErrorMessage = "Project type is required.")]
        [Display(Name = "Project Type")]
        public string ProjectType { get; set; } = "Project";

        [Range(1, 10, ErrorMessage = "Capacity must be between 1 and 10 teams.")]
        [Display(Name = "Team Capacity")]
        public int Capacity { get; set; } = 1;

        [Display(Name = "Open for Applications")]
        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Faculty Owner")]
        public string? FacultyOwnerId { get; set; }

        public string? FacultyOwnerDisplay { get; set; }

        public int CurrentAssignedCount { get; set; }

        public List<string> AvailableDomains { get; set; } = new();
        public List<string> ProjectTypes { get; set; } = new();
        public IEnumerable<SelectListItem> FacultyOptions { get; set; } = new List<SelectListItem>();
    }
}
