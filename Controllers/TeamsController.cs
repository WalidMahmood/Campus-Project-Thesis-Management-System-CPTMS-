using CPTMS.Data;
using CPTMS.Models.Entities;
using CPTMS.Models.ViewModels;
using CPTMS.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPTMS.Controllers
{
    [Authorize]
    public class TeamsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEligibilityService _eligibilityService;

        public TeamsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEligibilityService eligibilityService)
        {
            _context = context;
            _userManager = userManager;
            _eligibilityService = eligibilityService;
        }

        // GET: /Teams
        [HttpGet]
        public async Task<IActionResult> Index(string? search, string? projectType)
        {
            var currentUserId = _userManager.GetUserId(User);
            bool isStudent = User.IsInRole("Student");
            bool isCoordinator = User.IsInRole("Coordinator") || User.IsInRole("Administrator");

            var teamsQuery = _context.ProjectTeams
                .AsNoTracking()
                .Include(t => t.Members)
                    .ThenInclude(m => m.Student)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                teamsQuery = teamsQuery.Where(t =>
                    t.Name.ToLower().Contains(term) ||
                    t.Department.ToLower().Contains(term) ||
                    (t.Description != null && t.Description.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(projectType) && (projectType == "Project" || projectType == "Thesis"))
            {
                teamsQuery = teamsQuery.Where(t => t.ProjectType == projectType);
            }

            var teams = await teamsQuery
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var summaries = new List<TeamSummaryViewModel>();
            foreach (var team in teams)
            {
                var rule = await _eligibilityService.GetActiveRuleAsync(team.ProjectType, team.Department);
                var accepted = team.Members.Where(m => m.Status == "Accepted").ToList();
                var leader = accepted.FirstOrDefault(m => m.MembershipRole == "Leader")?.Student?.FullName ?? "Unassigned";

                summaries.Add(new TeamSummaryViewModel
                {
                    TeamId = team.TeamId,
                    Name = team.Name,
                    Department = team.Department,
                    ProjectType = team.ProjectType,
                    Description = team.Description,
                    LeaderName = leader,
                    AcceptedMemberCount = accepted.Count,
                    MinTeamSize = rule?.MinTeamSize ?? 2,
                    MaxTeamSize = rule?.MaxTeamSize ?? 4,
                    IsApproved = team.IsApproved,
                    IsLocked = team.IsLocked,
                    HasPendingRequestFromCurrentUser = currentUserId != null &&
                        team.Members.Any(m => m.StudentId == currentUserId && m.Status == "Requested"),
                    HasPendingInviteForCurrentUser = currentUserId != null &&
                        team.Members.Any(m => m.StudentId == currentUserId && m.Status == "Invited")
                });
            }

            var vm = new TeamsIndexViewModel
            {
                IsStudent = isStudent,
                IsCoordinator = isCoordinator,
                AllTeams = summaries,
                SearchQuery = search,
                ProjectTypeFilter = projectType
            };

            if (isStudent && currentUserId != null)
            {
                var myMemberships = await _context.TeamMemberships
                    .AsNoTracking()
                    .Include(m => m.Team)
                    .Where(m => m.StudentId == currentUserId)
                    .ToListAsync();

                var activeMembership = myMemberships.FirstOrDefault(m => m.Status == "Accepted");
                if (activeMembership != null)
                {
                    vm.MyActiveTeam = await _context.ProjectTeams
                        .AsNoTracking()
                        .Include(t => t.Members)
                            .ThenInclude(tm => tm.Student)
                        .FirstOrDefaultAsync(t => t.TeamId == activeMembership.TeamId);

                    if (vm.MyActiveTeam != null)
                    {
                        vm.MyTeamEligibility = await _eligibilityService.EvaluateTeamAsync(activeMembership.TeamId);
                    }
                }

                vm.PendingInvitations = myMemberships
                    .Where(m => m.Status == "Invited" && m.Team != null)
                    .OrderByDescending(m => m.JoinedAt)
                    .ToList();

                vm.MyPendingRequests = myMemberships
                    .Where(m => m.Status == "Requested" && m.Team != null)
                    .OrderByDescending(m => m.JoinedAt)
                    .ToList();
            }

            return View(vm);
        }

        // GET: /Teams/Create
        [HttpGet]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Create()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            bool alreadyInTeam = await _context.TeamMemberships
                .AnyAsync(m => m.StudentId == currentUser.Id && m.Status == "Accepted");

            if (alreadyInTeam)
            {
                TempData["ErrorMessage"] = "You are already an accepted member of a team. Leave your current team before creating a new one.";
                return RedirectToAction(nameof(Index));
            }

            var model = new CreateTeamViewModel
            {
                Department = currentUser.Department
            };

            return View(model);
        }

        // POST: /Teams/Create
        [HttpPost]
        [Authorize(Roles = "Student")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTeamViewModel model)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            if (model.ProjectType != "Project" && model.ProjectType != "Thesis")
            {
                ModelState.AddModelError(nameof(model.ProjectType), "Project type must be either 'Project' or 'Thesis'.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // 1. Ensure student is not already in an accepted team
            bool alreadyInTeam = await _context.TeamMemberships
                .AnyAsync(m => m.StudentId == currentUser.Id && m.Status == "Accepted");

            if (alreadyInTeam)
            {
                ModelState.AddModelError(string.Empty, "You are already an accepted member of an active team.");
                return View(model);
            }

            // 2. Enforce F5 Eligibility validation before allowing team creation
            var eligibility = await _eligibilityService.EvaluateStudentAsync(currentUser.Id, model.ProjectType);
            if (!eligibility.IsEligible)
            {
                foreach (var reason in eligibility.FailureReasons)
                {
                    ModelState.AddModelError(string.Empty, reason);
                }
                return View(model);
            }

            // 3. Enforce unique team name (case-insensitive)
            var normalizedName = model.Name.Trim().ToLower();
            bool duplicateName = await _context.ProjectTeams
                .AnyAsync(t => t.Name.ToLower() == normalizedName);

            if (duplicateName)
            {
                ModelState.AddModelError(nameof(model.Name), "A team with this name already exists. Please choose another name.");
                return View(model);
            }

            var team = new ProjectTeam
            {
                Name = model.Name.Trim(),
                ProjectType = model.ProjectType,
                Department = currentUser.Department,
                Description = model.Description?.Trim(),
                CreatedByStudentId = currentUser.Id,
                CreatedAt = DateTime.UtcNow,
                IsApproved = false,
                IsLocked = false
            };

            _context.ProjectTeams.Add(team);
            await _context.SaveChangesAsync();

            var leaderMembership = new TeamMembership
            {
                TeamId = team.TeamId,
                StudentId = currentUser.Id,
                MembershipRole = "Leader",
                Status = "Accepted",
                InitiatedBy = "Leader",
                JoinedAt = DateTime.UtcNow,
                RespondedAt = DateTime.UtcNow
            };

            _context.TeamMemberships.Add(leaderMembership);

            // Auto-close any prior pending invitations/requests for this student
            await RejectOtherPendingMembershipsAsync(currentUser.Id, team.TeamId);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Team '{team.Name}' created! You are assigned as the Team Leader.";
            return RedirectToAction(nameof(Details), new { id = team.TeamId });
        }

        // GET: /Teams/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var team = await _context.ProjectTeams
                .AsNoTracking()
                .Include(t => t.Members)
                    .ThenInclude(m => m.Student)
                .FirstOrDefaultAsync(t => t.TeamId == id);

            if (team == null)
            {
                return NotFound();
            }

            var currentUserId = _userManager.GetUserId(User);
            bool isStudent = User.IsInRole("Student");
            bool isCoordinator = User.IsInRole("Coordinator") || User.IsInRole("Administrator");

            var acceptedMembers = team.Members
                .Where(m => m.Status == "Accepted")
                .OrderByDescending(m => m.MembershipRole == "Leader")
                .ThenBy(m => m.JoinedAt)
                .ToList();

            var pendingRequests = team.Members
                .Where(m => m.Status == "Requested")
                .OrderByDescending(m => m.JoinedAt)
                .ToList();

            var pendingInvites = team.Members
                .Where(m => m.Status == "Invited")
                .OrderByDescending(m => m.JoinedAt)
                .ToList();

            bool isLeader = currentUserId != null && acceptedMembers.Any(m => m.StudentId == currentUserId && m.MembershipRole == "Leader");
            bool isAcceptedMember = currentUserId != null && acceptedMembers.Any(m => m.StudentId == currentUserId);
            bool hasAnyTeam = currentUserId != null && await _context.TeamMemberships
                .AnyAsync(m => m.StudentId == currentUserId && m.Status == "Accepted");

            var currentInvite = currentUserId != null
                ? pendingInvites.FirstOrDefault(m => m.StudentId == currentUserId)
                : null;

            var eligibility = await _eligibilityService.EvaluateTeamAsync(id);

            var availableStudents = new List<ApplicationUser>();
            if (isLeader && !team.IsLocked)
            {
                var busyStudentIds = await _context.TeamMemberships
                    .AsNoTracking()
                    .Where(m => m.Status == "Accepted" || (m.TeamId == id && (m.Status == "Invited" || m.Status == "Requested")))
                    .Select(m => m.StudentId)
                    .Distinct()
                    .ToListAsync();

                availableStudents = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.UserRole == "Student" && !busyStudentIds.Contains(u.Id))
                    .OrderBy(u => u.FullName)
                    .ToListAsync();
            }

            var vm = new TeamDetailsViewModel
            {
                Team = team,
                AcceptedMembers = acceptedMembers,
                PendingJoinRequests = pendingRequests,
                PendingInvitations = pendingInvites,
                EligibilityReport = eligibility,
                IsCurrentUserLeader = isLeader,
                IsCurrentUserAcceptedMember = isAcceptedMember,
                IsCurrentUserStudent = isStudent,
                CurrentUserHasAnyTeam = hasAnyTeam,
                CurrentUserHasPendingRequestHere = currentUserId != null && pendingRequests.Any(m => m.StudentId == currentUserId),
                CurrentUserPendingInviteId = currentInvite?.MembershipId,
                IsCoordinator = isCoordinator,
                CurrentUserId = currentUserId,
                AvailableStudentsToInvite = availableStudents
            };

            return View(vm);
        }

        // POST: /Teams/InviteMember
        [HttpPost]
        [Authorize(Roles = "Student")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteMember(int teamId, string identifier)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null)
            {
                return Challenge();
            }

            var team = await _context.ProjectTeams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
            {
                return NotFound();
            }

            bool isLeader = team.Members.Any(m => m.StudentId == currentUserId && m.Status == "Accepted" && m.MembershipRole == "Leader");
            if (!isLeader)
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(identifier))
            {
                TempData["ErrorMessage"] = "Please select a student or enter their email / Student ID.";
                return RedirectToAction(nameof(Details), new { id = teamId });
            }

            var trimmed = identifier.Trim();
            var lowered = trimmed.ToLower();

            var candidate = await _context.Users.FirstOrDefaultAsync(u =>
                u.Id == trimmed ||
                (u.Email != null && u.Email.ToLower() == lowered) ||
                (u.StudentIdNumber != null && u.StudentIdNumber.ToLower() == lowered));

            if (candidate == null || !candidate.UserRole.Equals("Student", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = $"No student account found matching '{trimmed}'.";
                return RedirectToAction(nameof(Details), new { id = teamId });
            }

            bool alreadyPendingOrMember = team.Members.Any(m =>
                m.StudentId == candidate.Id &&
                (m.Status == "Accepted" || m.Status == "Invited" || m.Status == "Requested"));

            if (alreadyPendingOrMember)
            {
                TempData["ErrorMessage"] = $"{candidate.FullName} is already a member or has a pending invite/request for this team.";
                return RedirectToAction(nameof(Details), new { id = teamId });
            }

            // Enforce F5 eligibility & team-size cap before inviting
            var check = await _eligibilityService.CanAddMemberToTeamAsync(teamId, candidate.Id);
            if (!check.IsEligible)
            {
                TempData["ErrorMessage"] = $"Cannot invite {candidate.FullName}: {string.Join(" ", check.FailureReasons)}";
                return RedirectToAction(nameof(Details), new { id = teamId });
            }

            _context.TeamMemberships.Add(new TeamMembership
            {
                TeamId = teamId,
                StudentId = candidate.Id,
                MembershipRole = "Member",
                Status = "Invited",
                InitiatedBy = "Leader",
                JoinedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Invitation sent to {candidate.FullName} ({candidate.StudentIdNumber ?? candidate.Email}).";
            return RedirectToAction(nameof(Details), new { id = teamId });
        }

        // POST: /Teams/RequestJoin
        [HttpPost]
        [Authorize(Roles = "Student")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestJoin(int teamId)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null)
            {
                return Challenge();
            }

            var team = await _context.ProjectTeams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
            {
                return NotFound();
            }

            bool alreadyAssociated = team.Members.Any(m =>
                m.StudentId == currentUserId &&
                (m.Status == "Accepted" || m.Status == "Invited" || m.Status == "Requested"));

            if (alreadyAssociated)
            {
                TempData["ErrorMessage"] = "You already have an active membership, invitation, or pending request with this team.";
                return RedirectToAction(nameof(Details), new { id = teamId });
            }

            // Enforce F5 eligibility & capacity check
            var check = await _eligibilityService.CanAddMemberToTeamAsync(teamId, currentUserId);
            if (!check.IsEligible)
            {
                TempData["ErrorMessage"] = $"Cannot request to join '{team.Name}': {string.Join(" ", check.FailureReasons)}";
                return RedirectToAction(nameof(Details), new { id = teamId });
            }

            _context.TeamMemberships.Add(new TeamMembership
            {
                TeamId = teamId,
                StudentId = currentUserId,
                MembershipRole = "Member",
                Status = "Requested",
                InitiatedBy = "Student",
                JoinedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Join request sent to '{team.Name}'. Waiting for Team Leader approval.";
            return RedirectToAction(nameof(Details), new { id = teamId });
        }

        // POST: /Teams/RespondToInvite
        [HttpPost]
        [Authorize(Roles = "Student")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RespondToInvite(int membershipId, string decision)
        {
            var currentUserId = _userManager.GetUserId(User);
            var membership = await _context.TeamMemberships
                .Include(m => m.Team)
                .FirstOrDefaultAsync(m => m.MembershipId == membershipId);

            if (membership == null)
            {
                return NotFound();
            }

            if (membership.StudentId != currentUserId || membership.Status != "Invited")
            {
                return Forbid();
            }

            if (decision.Equals("accept", StringComparison.OrdinalIgnoreCase))
            {
                var check = await _eligibilityService.CanAddMemberToTeamAsync(membership.TeamId, currentUserId!);
                if (!check.IsEligible)
                {
                    TempData["ErrorMessage"] = $"Cannot accept invitation: {string.Join(" ", check.FailureReasons)}";
                    return RedirectToAction(nameof(Index));
                }

                membership.Status = "Accepted";
                membership.RespondedAt = DateTime.UtcNow;

                await RejectOtherPendingMembershipsAsync(currentUserId!, membership.TeamId);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"You have joined team '{membership.Team?.Name}'!";
                return RedirectToAction(nameof(Details), new { id = membership.TeamId });
            }

            membership.Status = "Rejected";
            membership.RespondedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Invitation from '{membership.Team?.Name}' declined.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Teams/RespondToRequest
        [HttpPost]
        [Authorize(Roles = "Student,Coordinator,Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RespondToRequest(int membershipId, string decision)
        {
            var currentUserId = _userManager.GetUserId(User);
            var membership = await _context.TeamMemberships
                .Include(m => m.Team)
                    .ThenInclude(t => t!.Members)
                .Include(m => m.Student)
                .FirstOrDefaultAsync(m => m.MembershipId == membershipId);

            if (membership == null || membership.Team == null)
            {
                return NotFound();
            }

            bool isLeader = membership.Team.Members.Any(m => m.StudentId == currentUserId && m.Status == "Accepted" && m.MembershipRole == "Leader");
            bool isCoordinator = User.IsInRole("Coordinator") || User.IsInRole("Administrator");

            if ((!isLeader && !isCoordinator) || membership.Status != "Requested")
            {
                return Forbid();
            }

            if (decision.Equals("accept", StringComparison.OrdinalIgnoreCase))
            {
                var check = await _eligibilityService.CanAddMemberToTeamAsync(membership.TeamId, membership.StudentId);
                if (!check.IsEligible)
                {
                    TempData["ErrorMessage"] = $"Cannot accept {membership.Student?.FullName}: {string.Join(" ", check.FailureReasons)}";
                    return RedirectToAction(nameof(Details), new { id = membership.TeamId });
                }

                membership.Status = "Accepted";
                membership.RespondedAt = DateTime.UtcNow;

                await RejectOtherPendingMembershipsAsync(membership.StudentId, membership.TeamId);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"{membership.Student?.FullName} has been accepted into '{membership.Team.Name}'.";
                return RedirectToAction(nameof(Details), new { id = membership.TeamId });
            }

            membership.Status = "Rejected";
            membership.RespondedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Join request from {membership.Student?.FullName} was rejected.";
            return RedirectToAction(nameof(Details), new { id = membership.TeamId });
        }

        // POST: /Teams/CancelPending
        [HttpPost]
        [Authorize(Roles = "Student,Coordinator,Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelPending(int membershipId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var membership = await _context.TeamMemberships
                .Include(m => m.Team)
                    .ThenInclude(t => t!.Members)
                .FirstOrDefaultAsync(m => m.MembershipId == membershipId);

            if (membership == null || membership.Team == null)
            {
                return NotFound();
            }

            bool isSelfRequest = membership.Status == "Requested" && membership.StudentId == currentUserId;
            bool isLeaderCancelingInvite = membership.Status == "Invited" &&
                membership.Team.Members.Any(m => m.StudentId == currentUserId && m.Status == "Accepted" && m.MembershipRole == "Leader");
            bool isCoordinator = User.IsInRole("Coordinator") || User.IsInRole("Administrator");

            if (!isSelfRequest && !isLeaderCancelingInvite && !isCoordinator)
            {
                return Forbid();
            }

            int teamId = membership.TeamId;
            _context.TeamMemberships.Remove(membership);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Pending membership item cancelled.";
            return isSelfRequest
                ? RedirectToAction(nameof(Index))
                : RedirectToAction(nameof(Details), new { id = teamId });
        }

        // POST: /Teams/RemoveMember
        [HttpPost]
        [Authorize(Roles = "Student,Coordinator,Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int membershipId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var membership = await _context.TeamMemberships
                .Include(m => m.Team)
                    .ThenInclude(t => t!.Members)
                .Include(m => m.Student)
                .FirstOrDefaultAsync(m => m.MembershipId == membershipId);

            if (membership == null || membership.Team == null)
            {
                return NotFound();
            }

            bool isCoordinator = User.IsInRole("Coordinator") || User.IsInRole("Administrator");
            if (membership.Team.IsLocked && !isCoordinator)
            {
                TempData["ErrorMessage"] = "This team is locked. Contact the Department Coordinator for membership changes.";
                return RedirectToAction(nameof(Details), new { id = membership.TeamId });
            }

            bool isLeader = membership.Team.Members.Any(m => m.StudentId == currentUserId && m.Status == "Accepted" && m.MembershipRole == "Leader");
            bool isSelf = membership.StudentId == currentUserId;

            if (!isCoordinator && !isLeader && !isSelf)
            {
                return Forbid();
            }

            var otherAcceptedMembers = membership.Team.Members
                .Where(m => m.Status == "Accepted" && m.MembershipId != membershipId)
                .ToList();

            if (membership.MembershipRole == "Leader" && otherAcceptedMembers.Any() && !isCoordinator)
            {
                TempData["ErrorMessage"] = "Transfer team leadership to another member before leaving the team.";
                return RedirectToAction(nameof(Details), new { id = membership.TeamId });
            }

            membership.Status = "Removed";
            membership.RespondedAt = DateTime.UtcNow;

            // If Coordinator removes a leader while others remain, auto-promote the earliest member
            if (membership.MembershipRole == "Leader" && otherAcceptedMembers.Any())
            {
                var nextLeader = otherAcceptedMembers.OrderBy(m => m.JoinedAt).First();
                nextLeader.MembershipRole = "Leader";
            }

            // Re-open approval status if membership changed
            membership.Team.IsApproved = false;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = isSelf
                ? $"You have left team '{membership.Team.Name}'."
                : $"{membership.Student?.FullName} was removed from '{membership.Team.Name}'.";

            return isSelf
                ? RedirectToAction(nameof(Index))
                : RedirectToAction(nameof(Details), new { id = membership.TeamId });
        }

        // POST: /Teams/TransferLeadership
        [HttpPost]
        [Authorize(Roles = "Student,Coordinator,Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransferLeadership(int teamId, int newLeaderMembershipId)
        {
            var currentUserId = _userManager.GetUserId(User);
            var team = await _context.ProjectTeams
                .Include(t => t.Members)
                    .ThenInclude(m => m.Student)
                .FirstOrDefaultAsync(t => t.TeamId == teamId);

            if (team == null)
            {
                return NotFound();
            }

            bool isCoordinator = User.IsInRole("Coordinator") || User.IsInRole("Administrator");
            var currentLeader = team.Members.FirstOrDefault(m => m.Status == "Accepted" && m.MembershipRole == "Leader");

            if (!isCoordinator && (currentLeader == null || currentLeader.StudentId != currentUserId))
            {
                return Forbid();
            }

            var targetMember = team.Members.FirstOrDefault(m => m.MembershipId == newLeaderMembershipId && m.Status == "Accepted");
            if (targetMember == null)
            {
                TempData["ErrorMessage"] = "Selected student is not an accepted member of this team.";
                return RedirectToAction(nameof(Details), new { id = teamId });
            }

            if (currentLeader != null)
            {
                currentLeader.MembershipRole = "Member";
            }

            targetMember.MembershipRole = "Leader";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Team leadership transferred to {targetMember.Student?.FullName}.";
            return RedirectToAction(nameof(Details), new { id = teamId });
        }

        // POST: /Teams/ApproveTeam
        [HttpPost]
        [Authorize(Roles = "Coordinator,Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveTeam(int teamId)
        {
            var team = await _context.ProjectTeams.FindAsync(teamId);
            if (team == null)
            {
                return NotFound();
            }

            var eligibility = await _eligibilityService.EvaluateTeamAsync(teamId);
            if (!eligibility.IsEligible)
            {
                TempData["ErrorMessage"] = $"Cannot approve '{team.Name}' because it does not meet eligibility rules: {string.Join(" ", eligibility.FailureReasons)}";
                return RedirectToAction(nameof(Details), new { id = teamId });
            }

            team.IsApproved = true;
            team.IsLocked = true;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Team '{team.Name}' verified, approved, and locked for proposal submission.";
            return RedirectToAction(nameof(Details), new { id = teamId });
        }

        // POST: /Teams/UnlockTeam
        [HttpPost]
        [Authorize(Roles = "Coordinator,Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockTeam(int teamId)
        {
            var team = await _context.ProjectTeams.FindAsync(teamId);
            if (team == null)
            {
                return NotFound();
            }

            team.IsLocked = false;
            team.IsApproved = false;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Team '{team.Name}' unlocked for membership updates.";
            return RedirectToAction(nameof(Details), new { id = teamId });
        }

        private async Task RejectOtherPendingMembershipsAsync(string studentId, int acceptedTeamId)
        {
            var otherPending = await _context.TeamMemberships
                .Where(m => m.StudentId == studentId &&
                            m.TeamId != acceptedTeamId &&
                            (m.Status == "Invited" || m.Status == "Requested"))
                .ToListAsync();

            foreach (var item in otherPending)
            {
                item.Status = "Rejected";
                item.RespondedAt = DateTime.UtcNow;
            }
        }
    }
}
