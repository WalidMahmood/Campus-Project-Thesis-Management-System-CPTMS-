using CPTMS.Models.Entities;
using CPTMS.Models.ViewModels;

namespace CPTMS.Services
{
    public interface IEligibilityService
    {
        Task<EligibilityRule?> GetActiveRuleAsync(string projectType, string department);
        Task<EligibilityCheckResult> EvaluateStudentAsync(string studentId, string projectType);
        Task<EligibilityCheckResult> EvaluateTeamAsync(int teamId);
        Task<EligibilityCheckResult> CanAddMemberToTeamAsync(int teamId, string candidateStudentId);
    }
}
