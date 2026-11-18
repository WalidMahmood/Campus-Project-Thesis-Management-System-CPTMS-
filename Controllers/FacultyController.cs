using CPTMS.Data;
using CPTMS.Models.Entities;
using CPTMS.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPTMS.Controllers
{
    [Authorize]
    public class FacultyController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<FacultyController> _logger;

        public FacultyController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<FacultyController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: /Faculty/Availability
        [HttpGet]
        public async Task<IActionResult> Availability(string? searchQuery, bool hasOpenSlotsOnly = false)
        {
            var items = await BuildAvailabilityAsync();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var term = searchQuery.Trim();
                items = items.Where(i =>
                    Contains(i.FullName, term) ||
                    Contains(i.Designation, term) ||
                    Contains(i.Department, term) ||
                    Contains(i.ResearchInterests, term) ||
                    Contains(i.EligibilityConditions, term)).ToList();
            }

            if (hasOpenSlotsOnly)
            {
                items = items.Where(i => i.HasOpenSlots).ToList();
            }

            var model = new FacultyAvailabilityViewModel
            {
                SearchQuery = searchQuery,
                HasOpenSlotsOnly = hasOpenSlotsOnly,
                Results = items
                    .OrderByDescending(i => i.HasOpenSlots)
                    .ThenBy(i => i.FullName)
                    .ToList()
            };

            return View(model);
        }

        // GET: /Faculty/EditProfile
        [HttpGet]
        [Authorize(Roles = "Supervisor")]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var profile = await GetOrCreateProfileAsync(user);
            return View(BuildEditViewModel(profile, user));
        }

        // POST: /Faculty/EditProfile
        [HttpPost]
        [Authorize(Roles = "Supervisor")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(EditFacultyProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge();
            }

            var profile = await GetOrCreateProfileAsync(user);

            if (string.IsNullOrWhiteSpace(model.ResearchInterests))
            {
                ModelState.AddModelError(nameof(model.ResearchInterests), "Research interests are required.");
            }

            // Business rule 1: a faculty member cannot declare a load above the department-approved ceiling.
            if (model.MaxSupervisionLoad > profile.ApprovedCapacityLimit)
            {
                ModelState.AddModelError(nameof(model.MaxSupervisionLoad),
                    $"Declared load ({model.MaxSupervisionLoad}) cannot exceed your department-approved limit of {profile.ApprovedCapacityLimit}.");
            }

            // Business rule 2: the declared load can never drop below work already in progress.
            if (model.MaxSupervisionLoad < profile.CurrentSupervisionLoad)
            {
                ModelState.AddModelError(nameof(model.MaxSupervisionLoad),
                    $"Declared load cannot be lower than your current active supervision load ({profile.CurrentSupervisionLoad}).");
            }

            if (!ModelState.IsValid)
            {
                var viewModel = BuildEditViewModel(profile, user);
                viewModel.ResearchInterests = model.ResearchInterests;
                viewModel.EligibilityConditions = model.EligibilityConditions;
                viewModel.MaxSupervisionLoad = model.MaxSupervisionLoad;
                return View(viewModel);
            }

            profile.ResearchInterests = model.ResearchInterests.Trim();
            profile.EligibilityConditions = string.IsNullOrWhiteSpace(model.EligibilityConditions)
                ? null
                : model.EligibilityConditions.Trim();
            profile.MaxSupervisionLoad = model.MaxSupervisionLoad;
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Faculty profile for {Email} updated to a declared load of {Load}.",
                user.Email, profile.MaxSupervisionLoad);

            TempData["SuccessMessage"] = "Your supervision capacity and research interests were updated.";
            return RedirectToAction(nameof(Availability));
        }

        // GET: /Faculty/ManageLimits
        [HttpGet]
        [Authorize(Roles = "Coordinator")]
        public async Task<IActionResult> ManageLimits()
        {
            return View(await BuildManageLimitsAsync());
        }

        // POST: /Faculty/ManageLimits
        [HttpPost]
        [Authorize(Roles = "Coordinator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ManageLimits(UpdateApprovedLimitViewModel model)
        {
            var targetUser = string.IsNullOrWhiteSpace(model.UserId)
                ? null
                : await _userManager.FindByIdAsync(model.UserId);

            if (targetUser == null || !await _userManager.IsInRoleAsync(targetUser, "Supervisor"))
            {
                ModelState.AddModelError(string.Empty, "Please choose a valid supervisor.");
                return View(await BuildManageLimitsAsync());
            }

            var profile = await _context.FacultyProfiles.FirstOrDefaultAsync(p => p.UserId == targetUser.Id);
            if (profile == null)
            {
                profile = new FacultyProfile
                {
                    UserId = targetUser.Id,
                    Department = string.IsNullOrWhiteSpace(targetUser.Department)
                        ? "Computer Science and Engineering"
                        : targetUser.Department,
                    ResearchInterests = string.IsNullOrWhiteSpace(targetUser.ResearchInterests)
                        ? "Not specified"
                        : targetUser.ResearchInterests,
                    MaxSupervisionLoad = 0,
                    ApprovedCapacityLimit = 0,
                    CurrentSupervisionLoad = 0
                };
                _context.FacultyProfiles.Add(profile);
            }

            // An approved ceiling can never be pushed below work already in progress.
            if (model.ApprovedCapacityLimit < profile.CurrentSupervisionLoad)
            {
                ModelState.AddModelError(nameof(model.ApprovedCapacityLimit),
                    $"Approved limit cannot be lower than {targetUser.FullName}'s current load of {profile.CurrentSupervisionLoad}.");
            }

            if (!ModelState.IsValid)
            {
                return View(await BuildManageLimitsAsync());
            }

            var clampedDeclaredLoad = false;
            if (profile.MaxSupervisionLoad > model.ApprovedCapacityLimit)
            {
                profile.MaxSupervisionLoad = model.ApprovedCapacityLimit;
                clampedDeclaredLoad = true;
            }

            profile.ApprovedCapacityLimit = model.ApprovedCapacityLimit;
            profile.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Coordinator set approved capacity for {Email} to {Limit}.",
                targetUser.Email, profile.ApprovedCapacityLimit);

            TempData["SuccessMessage"] =
                $"Approved capacity for {targetUser.FullName} is now {profile.ApprovedCapacityLimit}.";

            if (clampedDeclaredLoad)
            {
                TempData["WarningMessage"] =
                    $"{targetUser.FullName}'s declared load was reduced to {profile.MaxSupervisionLoad} to stay within the new approved limit.";
            }

            return RedirectToAction(nameof(ManageLimits));
        }

        private async Task<List<FacultyAvailabilityItemViewModel>> BuildAvailabilityAsync()
        {
            var supervisors = await _userManager.GetUsersInRoleAsync("Supervisor");
            var profiles = await _context.FacultyProfiles
                .AsNoTracking()
                .ToDictionaryAsync(p => p.UserId);
            var topicCounts = await _context.Topics
                .AsNoTracking()
                .GroupBy(t => t.FacultyOwnerId)
                .Select(g => new { OwnerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.OwnerId, x => x.Count);

            return supervisors.Select(supervisor =>
            {
                profiles.TryGetValue(supervisor.Id, out var profile);

                return new FacultyAvailabilityItemViewModel
                {
                    FacultyProfileId = profile?.FacultyProfileId,
                    UserId = supervisor.Id,
                    FullName = supervisor.FullName,
                    Designation = supervisor.Designation,
                    Email = supervisor.Email ?? string.Empty,
                    Department = profile?.Department ?? supervisor.Department,
                    ResearchInterests = profile?.ResearchInterests
                        ?? (string.IsNullOrWhiteSpace(supervisor.ResearchInterests) ? "Not specified" : supervisor.ResearchInterests),
                    EligibilityConditions = profile?.EligibilityConditions,
                    CurrentSupervisionLoad = profile?.CurrentSupervisionLoad ?? 0,
                    MaxSupervisionLoad = profile?.MaxSupervisionLoad ?? 0,
                    ApprovedCapacityLimit = profile?.ApprovedCapacityLimit ?? 0,
                    RemainingSlots = Math.Max(0, (profile?.MaxSupervisionLoad ?? 0) - (profile?.CurrentSupervisionLoad ?? 0)),
                    TopicCount = topicCounts.TryGetValue(supervisor.Id, out var count) ? count : 0
                };
            }).ToList();
        }

        private async Task<List<UpdateApprovedLimitViewModel>> BuildManageLimitsAsync()
        {
            var supervisors = await _userManager.GetUsersInRoleAsync("Supervisor");
            var profiles = await _context.FacultyProfiles
                .AsNoTracking()
                .ToDictionaryAsync(p => p.UserId);

            return supervisors
                .OrderBy(s => s.FullName)
                .Select(supervisor =>
                {
                    profiles.TryGetValue(supervisor.Id, out var profile);

                    return new UpdateApprovedLimitViewModel
                    {
                        FacultyProfileId = profile?.FacultyProfileId,
                        UserId = supervisor.Id,
                        FullName = supervisor.FullName,
                        Designation = supervisor.Designation,
                        Department = profile?.Department ?? supervisor.Department,
                        CurrentSupervisionLoad = profile?.CurrentSupervisionLoad ?? 0,
                        MaxSupervisionLoad = profile?.MaxSupervisionLoad ?? 0,
                        ApprovedCapacityLimit = profile?.ApprovedCapacityLimit ?? 0
                    };
                })
                .ToList();
        }

        private async Task<FacultyProfile> GetOrCreateProfileAsync(ApplicationUser user)
        {
            var profile = await _context.FacultyProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile != null)
            {
                return profile;
            }

            // Newly registered supervisors start with no approved capacity until the Coordinator grants it.
            profile = new FacultyProfile
            {
                UserId = user.Id,
                Department = string.IsNullOrWhiteSpace(user.Department)
                    ? "Computer Science and Engineering"
                    : user.Department,
                ResearchInterests = string.IsNullOrWhiteSpace(user.ResearchInterests)
                    ? "Not specified"
                    : user.ResearchInterests,
                EligibilityConditions = null,
                MaxSupervisionLoad = 0,
                ApprovedCapacityLimit = 0,
                CurrentSupervisionLoad = 0,
                UpdatedAt = DateTime.UtcNow
            };

            _context.FacultyProfiles.Add(profile);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Initialized faculty profile for {Email}.", user.Email);
            return profile;
        }

        private static EditFacultyProfileViewModel BuildEditViewModel(FacultyProfile profile, ApplicationUser user)
        {
            return new EditFacultyProfileViewModel
            {
                FullName = user.FullName,
                Department = profile.Department,
                ResearchInterests = profile.ResearchInterests,
                EligibilityConditions = profile.EligibilityConditions,
                MaxSupervisionLoad = profile.MaxSupervisionLoad,
                ApprovedCapacityLimit = profile.ApprovedCapacityLimit,
                CurrentSupervisionLoad = profile.CurrentSupervisionLoad
            };
        }

        private static bool Contains(string? source, string term)
        {
            return !string.IsNullOrWhiteSpace(source) &&
                   source.Contains(term, StringComparison.OrdinalIgnoreCase);
        }
    }
}
