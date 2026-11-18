using CPTMS.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace CPTMS.Models.ViewModels
{
    public class EligibilityCriterionResult
    {
        public string Criterion { get; set; } = string.Empty;
        public string RequiredValue { get; set; } = string.Empty;
        public string ActualValue { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string? Note { get; set; }
    }

    public class EligibilityCheckResult
    {
        public bool IsEligible { get; set; }
        public string SubjectLabel { get; set; } = string.Empty;
        public string ProjectType { get; set; } = "Project";
        public string RuleName { get; set; } = "No Active Rule";
        public int? RuleId { get; set; }
        public int MinTeamSize { get; set; } = 1;
        public int MaxTeamSize { get; set; } = 4;
        public List<EligibilityCriterionResult> Criteria { get; set; } = new();
        public List<string> FailureReasons => Criteria
            .Where(c => !c.Passed)
            .Select(c => string.IsNullOrWhiteSpace(c.Note)
                ? $"{c.Criterion}: required {c.RequiredValue}, got {c.ActualValue}."
                : c.Note!)
            .ToList();
    }

    public class EligibilityRuleFormViewModel : IValidatableObject
    {
        public int RuleId { get; set; }

        [Required(ErrorMessage = "Rule name is required.")]
        [StringLength(120)]
        [Display(Name = "Rule Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Project type is required.")]
        [Display(Name = "Project Type")]
        public string ProjectType { get; set; } = "Project";

        [Required(ErrorMessage = "Department is required.")]
        [StringLength(100)]
        [Display(Name = "Department")]
        public string Department { get; set; } = "Computer Science and Engineering";

        [Range(1, 10, ErrorMessage = "Minimum team size must be between 1 and 10.")]
        [Display(Name = "Minimum Team Size")]
        public int MinTeamSize { get; set; } = 2;

        [Range(1, 10, ErrorMessage = "Maximum team size must be between 1 and 10.")]
        [Display(Name = "Maximum Team Size")]
        public int MaxTeamSize { get; set; } = 4;

        [Range(1, 16, ErrorMessage = "Minimum semester must be between 1 and 16.")]
        [Display(Name = "Minimum Semester")]
        public int MinSemester { get; set; } = 7;

        [Range(0.0, 4.0, ErrorMessage = "Minimum CGPA must be between 0.00 and 4.00.")]
        [Display(Name = "Minimum CGPA")]
        public double MinCgpa { get; set; } = 2.50;

        [Range(0, 200, ErrorMessage = "Minimum completed credits must be between 0 and 200.")]
        [Display(Name = "Minimum Completed Credits")]
        public int MinCompletedCredits { get; set; } = 75;

        [Display(Name = "Require all team members to belong to the same department")]
        public bool RequireSameDepartment { get; set; } = true;

        [Display(Name = "Rule is active")]
        public bool IsActive { get; set; } = true;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (MinTeamSize > MaxTeamSize)
            {
                yield return new ValidationResult(
                    "Minimum team size cannot exceed maximum team size.",
                    new[] { nameof(MinTeamSize), nameof(MaxTeamSize) });
            }

            if (ProjectType != "Project" && ProjectType != "Thesis")
            {
                yield return new ValidationResult(
                    "Project type must be either 'Project' or 'Thesis'.",
                    new[] { nameof(ProjectType) });
            }
        }
    }

    public class EligibilityDashboardViewModel
    {
        public List<EligibilityRule> Rules { get; set; } = new();
        public bool IsStudent { get; set; }
        public bool CanManageRules { get; set; }
        public ApplicationUser? CurrentUser { get; set; }
        public EligibilityCheckResult? StudentProjectEligibility { get; set; }
        public EligibilityCheckResult? StudentThesisEligibility { get; set; }
        public List<ProjectTeam> Teams { get; set; } = new();
    }

    public class TeamEligibilityReportViewModel
    {
        public ProjectTeam Team { get; set; } = null!;
        public EligibilityCheckResult Result { get; set; } = new();
    }
}
