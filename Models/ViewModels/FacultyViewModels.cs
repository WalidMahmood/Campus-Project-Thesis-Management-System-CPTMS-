using System.ComponentModel.DataAnnotations;

namespace CPTMS.Models.ViewModels
{
    public class FacultyAvailabilityViewModel
    {
        [Display(Name = "Search by name, interest, or eligibility")]
        public string? SearchQuery { get; set; }

        [Display(Name = "Only faculty with open slots")]
        public bool HasOpenSlotsOnly { get; set; }

        public List<FacultyAvailabilityItemViewModel> Results { get; set; } = new();
    }

    public class FacultyAvailabilityItemViewModel
    {
        public int? FacultyProfileId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Designation { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string ResearchInterests { get; set; } = string.Empty;
        public string? EligibilityConditions { get; set; }
        public int CurrentSupervisionLoad { get; set; }
        public int MaxSupervisionLoad { get; set; }
        public int ApprovedCapacityLimit { get; set; }
        public int RemainingSlots { get; set; }
        public int TopicCount { get; set; }

        public bool HasOpenSlots =>
            CurrentSupervisionLoad < MaxSupervisionLoad &&
            CurrentSupervisionLoad < ApprovedCapacityLimit;

        public int LoadPercentage =>
            MaxSupervisionLoad <= 0
                ? (CurrentSupervisionLoad > 0 ? 100 : 0)
                : (int)Math.Round(CurrentSupervisionLoad * 100.0 / MaxSupervisionLoad);
    }

    public class EditFacultyProfileViewModel
    {
        [Required(ErrorMessage = "Research interests are required.")]
        [StringLength(500, ErrorMessage = "Research interests cannot exceed 500 characters.")]
        [Display(Name = "Research Interests / Domains")]
        public string ResearchInterests { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Eligibility conditions cannot exceed 500 characters.")]
        [Display(Name = "Eligibility Conditions")]
        public string? EligibilityConditions { get; set; }

        [Range(0, 20, ErrorMessage = "Declared load must be between 0 and 20.")]
        [Display(Name = "Declared Maximum Supervision Load")]
        public int MaxSupervisionLoad { get; set; }

        public int ApprovedCapacityLimit { get; set; }
        public int CurrentSupervisionLoad { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
    }

    public class UpdateApprovedLimitViewModel
    {
        public int? FacultyProfileId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;
        public string? Designation { get; set; }
        public string Department { get; set; } = string.Empty;
        public int CurrentSupervisionLoad { get; set; }
        public int MaxSupervisionLoad { get; set; }

        [Range(0, 20, ErrorMessage = "Approved limit must be between 0 and 20.")]
        [Display(Name = "Approved Capacity Limit")]
        public int ApprovedCapacityLimit { get; set; }
    }
}
