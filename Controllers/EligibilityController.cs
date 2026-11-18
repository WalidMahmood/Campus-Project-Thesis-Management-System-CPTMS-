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
    public class EligibilityController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEligibilityService _eligibilityService;

        public EligibilityController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IEligibilityService eligibilityService)
        {
            _context = context;
            _userManager = userManager;
            _eligibilityService = eligibilityService;
        }

        // GET: /Eligibility
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var rules = await _context.EligibilityRules
                .AsNoTracking()
                .OrderByDescending(r => r.IsActive)
                .ThenBy(r => r.ProjectType)
                .ThenBy(r => r.Department)
                .ToListAsync();

            var viewModel = new EligibilityDashboardViewModel
            {
                Rules = rules,
                CurrentUser = currentUser,
                IsStudent = User.IsInRole("Student"),
                CanManageRules = User.IsInRole("Coordinator") || User.IsInRole("Administrator")
            };

            if (viewModel.IsStudent && currentUser != null)
            {
                viewModel.StudentProjectEligibility = await _eligibilityService.EvaluateStudentAsync(currentUser.Id, "Project");
                viewModel.StudentThesisEligibility = await _eligibilityService.EvaluateStudentAsync(currentUser.Id, "Thesis");
            }

            if (viewModel.CanManageRules || User.IsInRole("Supervisor"))
            {
                viewModel.Teams = await _context.ProjectTeams
                    .AsNoTracking()
                    .Include(t => t.Members)
                    .OrderByDescending(t => t.CreatedAt)
                    .ToListAsync();
            }

            return View(viewModel);
        }

        // GET: /Eligibility/CheckTeam/5
        [HttpGet]
        public async Task<IActionResult> CheckTeam(int id)
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
            bool isPrivileged = User.IsInRole("Coordinator") || User.IsInRole("Administrator") || User.IsInRole("Supervisor");
            bool isTeamMember = team.Members.Any(m => m.StudentId == currentUserId && m.Status == "Accepted");

            if (!isPrivileged && !isTeamMember)
            {
                return Forbid();
            }

            var result = await _eligibilityService.EvaluateTeamAsync(id);
            return View(new TeamEligibilityReportViewModel
            {
                Team = team,
                Result = result
            });
        }

        // GET: /Eligibility/Create
        [HttpGet]
        [Authorize(Roles = "Coordinator,Administrator")]
        public IActionResult Create()
        {
            return View(new EligibilityRuleFormViewModel());
        }

        // POST: /Eligibility/Create
        [HttpPost]
        [Authorize(Roles = "Coordinator,Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EligibilityRuleFormViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var rule = new EligibilityRule
            {
                Name = model.Name.Trim(),
                ProjectType = model.ProjectType,
                Department = model.Department.Trim(),
                MinTeamSize = model.MinTeamSize,
                MaxTeamSize = model.MaxTeamSize,
                MinSemester = model.MinSemester,
                MinCgpa = model.MinCgpa,
                MinCompletedCredits = model.MinCompletedCredits,
                RequireSameDepartment = model.RequireSameDepartment,
                IsActive = model.IsActive,
                UpdatedAt = DateTime.UtcNow
            };

            _context.EligibilityRules.Add(rule);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Eligibility rule '{rule.Name}' created successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Eligibility/Edit/5
        [HttpGet]
        [Authorize(Roles = "Coordinator,Administrator")]
        public async Task<IActionResult> Edit(int id)
        {
            var rule = await _context.EligibilityRules.FindAsync(id);
            if (rule == null)
            {
                return NotFound();
            }

            var model = new EligibilityRuleFormViewModel
            {
                RuleId = rule.RuleId,
                Name = rule.Name,
                ProjectType = rule.ProjectType,
                Department = rule.Department,
                MinTeamSize = rule.MinTeamSize,
                MaxTeamSize = rule.MaxTeamSize,
                MinSemester = rule.MinSemester,
                MinCgpa = rule.MinCgpa,
                MinCompletedCredits = rule.MinCompletedCredits,
                RequireSameDepartment = rule.RequireSameDepartment,
                IsActive = rule.IsActive
            };

            return View(model);
        }

        // POST: /Eligibility/Edit/5
        [HttpPost]
        [Authorize(Roles = "Coordinator,Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, EligibilityRuleFormViewModel model)
        {
            if (id != model.RuleId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var rule = await _context.EligibilityRules.FindAsync(id);
            if (rule == null)
            {
                return NotFound();
            }

            rule.Name = model.Name.Trim();
            rule.ProjectType = model.ProjectType;
            rule.Department = model.Department.Trim();
            rule.MinTeamSize = model.MinTeamSize;
            rule.MaxTeamSize = model.MaxTeamSize;
            rule.MinSemester = model.MinSemester;
            rule.MinCgpa = model.MinCgpa;
            rule.MinCompletedCredits = model.MinCompletedCredits;
            rule.RequireSameDepartment = model.RequireSameDepartment;
            rule.IsActive = model.IsActive;
            rule.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Eligibility rule '{rule.Name}' updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Eligibility/ToggleActive/5
        [HttpPost]
        [Authorize(Roles = "Coordinator,Administrator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var rule = await _context.EligibilityRules.FindAsync(id);
            if (rule == null)
            {
                return NotFound();
            }

            rule.IsActive = !rule.IsActive;
            rule.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Rule '{rule.Name}' is now {(rule.IsActive ? "Active" : "Inactive")}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
