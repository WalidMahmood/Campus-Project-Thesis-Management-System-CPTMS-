using CPTMS.Data;
using CPTMS.Models.Entities;
using CPTMS.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CPTMS.Services
{
    public class EligibilityService : IEligibilityService
    {
        private readonly ApplicationDbContext _context;

        public EligibilityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<EligibilityRule?> GetActiveRuleAsync(string projectType, string department)
        {
            var activeRules = await _context.EligibilityRules
                .AsNoTracking()
                .Where(r => r.IsActive)
                .OrderByDescending(r => r.UpdatedAt)
                .ToListAsync();

            var exactMatch = activeRules.FirstOrDefault(r =>
                r.ProjectType.Equals(projectType, StringComparison.OrdinalIgnoreCase) &&
                r.Department.Equals(department, StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
            {
                return exactMatch;
            }

            return activeRules.FirstOrDefault(r =>
                r.ProjectType.Equals(projectType, StringComparison.OrdinalIgnoreCase) &&
                r.Department.Equals("Any", StringComparison.OrdinalIgnoreCase));
        }

        public async Task<EligibilityCheckResult> EvaluateStudentAsync(string studentId, string projectType)
        {
            var student = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == studentId);

            if (student == null)
            {
                return BuildErrorResult("Unknown Student", projectType, "Student account could not be found.");
            }

            var rule = await GetActiveRuleAsync(projectType, student.Department);
            if (rule == null)
            {
                return BuildErrorResult(
                    student.FullName,
                    projectType,
                    $"No active eligibility rule is configured for {projectType} in {student.Department}. Contact the Department Coordinator.");
            }

            var result = new EligibilityCheckResult
            {
                SubjectLabel = $"{student.FullName} ({student.StudentIdNumber ?? student.Email})",
                ProjectType = rule.ProjectType,
                RuleName = rule.Name,
                RuleId = rule.RuleId,
                MinTeamSize = rule.MinTeamSize,
                MaxTeamSize = rule.MaxTeamSize
            };

            AddStudentAcademicCriteria(result, student, rule, prefix: string.Empty);
            result.IsEligible = result.Criteria.All(c => c.Passed);
            return result;
        }

        public async Task<EligibilityCheckResult> EvaluateTeamAsync(int teamId)
        {
            var team = await _context.ProjectTeams
                .AsNoTracking()
                .Include(t => t.Members)
                    .ThenInclude(m => m.Student)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
            {
                return BuildErrorResult("Unknown Team", "Project", "The requested project team does not exist.");
            }

            var rule = await GetActiveRuleAsync(team.ProjectType, team.Department);
            if (rule == null)
            {
                return BuildErrorResult(
                    team.Name,
                    team.ProjectType,
                    $"No active eligibility rule is configured for {team.ProjectType} in {team.Department}. Contact the Department Coordinator.");
            }

            var acceptedMembers = team.Members
                .Where(m => m.Status == "Accepted" && m.Student != null)
                .OrderByDescending(m => m.MembershipRole == "Leader")
                .ThenBy(m => m.JoinedAt)
                .ToList();

            var result = new EligibilityCheckResult
            {
                SubjectLabel = team.Name,
                ProjectType = team.ProjectType,
                RuleName = rule.Name,
                RuleId = rule.RuleId,
                MinTeamSize = rule.MinTeamSize,
                MaxTeamSize = rule.MaxTeamSize
            };

            // 1. Team-size range check
            bool sizePassed = acceptedMembers.Count >= rule.MinTeamSize && acceptedMembers.Count <= rule.MaxTeamSize;
            result.Criteria.Add(new EligibilityCriterionResult
            {
                Criterion = "Team Size Range",
                RequiredValue = $"{rule.MinTeamSize} – {rule.MaxTeamSize} members",
                ActualValue = $"{acceptedMembers.Count} accepted member(s)",
                Passed = sizePassed,
                Note = sizePassed
                    ? null
                    : acceptedMembers.Count < rule.MinTeamSize
                        ? $"Team needs at least {rule.MinTeamSize - acceptedMembers.Count} more accepted member(s) to meet the minimum size of {rule.MinTeamSize}."
                        : $"Team has {acceptedMembers.Count} members, exceeding the maximum allowed size of {rule.MaxTeamSize}."
            });

            // 2. Same-department check (if required by rule)
            if (rule.RequireSameDepartment)
            {
                var mismatched = acceptedMembers
                    .Where(m => !m.Student!.Department.Equals(team.Department, StringComparison.OrdinalIgnoreCase))
                    .Select(m => $"{m.Student!.FullName} ({m.Student.Department})")
                    .ToList();

                result.Criteria.Add(new EligibilityCriterionResult
                {
                    Criterion = "Department Consistency",
                    RequiredValue = $"All members in {team.Department}",
                    ActualValue = mismatched.Count == 0
                        ? $"All {acceptedMembers.Count} member(s) match {team.Department}"
                        : $"Mismatch: {string.Join(", ", mismatched)}",
                    Passed = mismatched.Count == 0,
                    Note = mismatched.Count == 0
                        ? null
                        : $"All members must belong to {team.Department}. Mismatched: {string.Join(", ", mismatched)}."
                });
            }

            // 3. Individual member academic criteria
            foreach (var member in acceptedMembers)
            {
                AddStudentAcademicCriteria(
                    result,
                    member.Student!,
                    rule,
                    prefix: $"{member.Student!.FullName} — ");
            }

            result.IsEligible = result.Criteria.All(c => c.Passed);
            return result;
        }

        public async Task<EligibilityCheckResult> CanAddMemberToTeamAsync(int teamId, string candidateStudentId)
        {
            var team = await _context.ProjectTeams
                .AsNoTracking()
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
            {
                return BuildErrorResult("Unknown Team", "Project", "Team does not exist.");
            }

            var candidate = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == candidateStudentId);

            if (candidate == null)
            {
                return BuildErrorResult(team.Name, team.ProjectType, "Candidate student account could not be found.");
            }

            var rule = await GetActiveRuleAsync(team.ProjectType, team.Department);
            if (rule == null)
            {
                return BuildErrorResult(
                    candidate.FullName,
                    team.ProjectType,
                    $"No active eligibility rule exists for {team.ProjectType} in {team.Department}.");
            }

            var acceptedCount = team.Members.Count(m => m.Status == "Accepted");

            var result = new EligibilityCheckResult
            {
                SubjectLabel = $"{candidate.FullName} -> {team.Name}",
                ProjectType = team.ProjectType,
                RuleName = rule.Name,
                RuleId = rule.RuleId,
                MinTeamSize = rule.MinTeamSize,
                MaxTeamSize = rule.MaxTeamSize
            };

            // 1. Team lock check
            result.Criteria.Add(new EligibilityCriterionResult
            {
                Criterion = "Team Membership Open",
                RequiredValue = "Unlocked team",
                ActualValue = team.IsLocked ? "Team is locked" : "Open for changes",
                Passed = !team.IsLocked,
                Note = team.IsLocked ? "This team is locked and cannot accept new members." : null
            });

            // 2. Max team-size capacity check
            bool hasCapacity = acceptedCount + 1 <= rule.MaxTeamSize;
            result.Criteria.Add(new EligibilityCriterionResult
            {
                Criterion = "Maximum Team Capacity",
                RequiredValue = $"At most {rule.MaxTeamSize} members",
                ActualValue = $"{acceptedCount} / {rule.MaxTeamSize} current members",
                Passed = hasCapacity,
                Note = hasCapacity
                    ? null
                    : $"Team '{team.Name}' has already reached the maximum team size limit of {rule.MaxTeamSize}."
            });

            // 3. Single-team rule check (student cannot be an Accepted member of another team)
            var existingAcceptedTeam = await _context.TeamMemberships
                .AsNoTracking()
                .Include(m => m.Team)
                .FirstOrDefaultAsync(m => m.StudentId == candidateStudentId && m.Status == "Accepted");

            bool notInAnotherTeam = existingAcceptedTeam == null;
            result.Criteria.Add(new EligibilityCriterionResult
            {
                Criterion = "Single Active Team Limit",
                RequiredValue = "Not already an accepted member of a team",
                ActualValue = notInAnotherTeam
                    ? "No active team"
                    : $"Already in '{existingAcceptedTeam!.Team?.Name ?? "another team"}'",
                Passed = notInAnotherTeam,
                Note = notInAnotherTeam
                    ? null
                    : $"{candidate.FullName} is already an accepted member of team '{existingAcceptedTeam!.Team?.Name}'."
            });

            // 4. Same-department check
            if (rule.RequireSameDepartment)
            {
                bool sameDept = candidate.Department.Equals(team.Department, StringComparison.OrdinalIgnoreCase);
                result.Criteria.Add(new EligibilityCriterionResult
                {
                    Criterion = "Department Match",
                    RequiredValue = team.Department,
                    ActualValue = candidate.Department,
                    Passed = sameDept,
                    Note = sameDept
                        ? null
                        : $"{candidate.FullName} is in {candidate.Department}, which does not match team department {team.Department}."
                });
            }

            // 5. Individual academic eligibility checks
            AddStudentAcademicCriteria(result, candidate, rule, prefix: string.Empty);

            result.IsEligible = result.Criteria.All(c => c.Passed);
            return result;
        }

        private static void AddStudentAcademicCriteria(
            EligibilityCheckResult result,
            ApplicationUser student,
            EligibilityRule rule,
            string prefix)
        {
            // Department eligibility against rule
            bool deptAllowed = rule.Department.Equals("Any", StringComparison.OrdinalIgnoreCase)
                || student.Department.Equals(rule.Department, StringComparison.OrdinalIgnoreCase);

            result.Criteria.Add(new EligibilityCriterionResult
            {
                Criterion = $"{prefix}Department",
                RequiredValue = rule.Department,
                ActualValue = student.Department,
                Passed = deptAllowed,
                Note = deptAllowed
                    ? null
                    : $"{student.FullName}'s department ({student.Department}) does not satisfy rule requirement ({rule.Department})."
            });

            // Semester threshold
            bool semesterOk = student.Semester.HasValue && student.Semester.Value >= rule.MinSemester;
            result.Criteria.Add(new EligibilityCriterionResult
            {
                Criterion = $"{prefix}Semester Threshold",
                RequiredValue = $"Semester >= {rule.MinSemester}",
                ActualValue = student.Semester.HasValue ? $"Semester {student.Semester.Value}" : "Not recorded",
                Passed = semesterOk,
                Note = semesterOk
                    ? null
                    : $"{student.FullName} does not meet the minimum semester requirement (required >= {rule.MinSemester}, actual: {(student.Semester.HasValue ? student.Semester.Value.ToString() : "N/A")})."
            });

            // CGPA threshold
            bool cgpaOk = student.CGPA.HasValue && student.CGPA.Value >= rule.MinCgpa;
            result.Criteria.Add(new EligibilityCriterionResult
            {
                Criterion = $"{prefix}CGPA Threshold",
                RequiredValue = $"CGPA >= {rule.MinCgpa:F2}",
                ActualValue = student.CGPA.HasValue ? $"{student.CGPA.Value:F2}" : "Not recorded",
                Passed = cgpaOk,
                Note = cgpaOk
                    ? null
                    : $"{student.FullName} does not meet the minimum CGPA requirement (required >= {rule.MinCgpa:F2}, actual: {(student.CGPA.HasValue ? student.CGPA.Value.ToString("F2") : "N/A")})."
            });

            // Completed Credits threshold
            bool creditsOk = student.CompletedCredits.HasValue && student.CompletedCredits.Value >= rule.MinCompletedCredits;
            result.Criteria.Add(new EligibilityCriterionResult
            {
                Criterion = $"{prefix}Completed Credits",
                RequiredValue = $">= {rule.MinCompletedCredits} credits",
                ActualValue = student.CompletedCredits.HasValue ? $"{student.CompletedCredits.Value} credits" : "Not recorded",
                Passed = creditsOk,
                Note = creditsOk
                    ? null
                    : $"{student.FullName} does not meet minimum completed credits (required >= {rule.MinCompletedCredits}, actual: {(student.CompletedCredits.HasValue ? student.CompletedCredits.Value.ToString() : "N/A")})."
            });
        }

        private static EligibilityCheckResult BuildErrorResult(string subject, string projectType, string message)
        {
            return new EligibilityCheckResult
            {
                IsEligible = false,
                SubjectLabel = subject,
                ProjectType = projectType,
                RuleName = "Validation Failed",
                Criteria = new List<EligibilityCriterionResult>
                {
                    new EligibilityCriterionResult
                    {
                        Criterion = "Prerequisite Check",
                        RequiredValue = "Valid configuration & record",
                        ActualValue = "Missing or invalid",
                        Passed = false,
                        Note = message
                    }
                }
            };
        }
    }
}
