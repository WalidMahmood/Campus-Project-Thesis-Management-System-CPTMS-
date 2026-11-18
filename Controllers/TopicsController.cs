using CPTMS.Data;
using CPTMS.Models.Entities;
using CPTMS.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CPTMS.Controllers
{
    [Authorize]
    public class TopicsController : Controller
    {
        public static readonly string[] ProjectDomains =
        {
            "Artificial Intelligence",
            "Software Engineering",
            "Cybersecurity",
            "Web & Distributed Systems",
            "Data Science"
        };

        public static readonly string[] ProjectTypes = { "Project", "Thesis" };

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TopicsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Topics
        [HttpGet]
        public async Task<IActionResult> Index(TopicSearchViewModel filters)
        {
            var query = _context.Topics
                .AsNoTracking()
                .Include(t => t.FacultyOwner)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filters.SearchQuery))
            {
                var term = filters.SearchQuery.Trim().ToLower();
                query = query.Where(t =>
                    t.Title.ToLower().Contains(term) ||
                    t.Description.ToLower().Contains(term) ||
                    t.RequiredSkills.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(filters.SelectedDomain))
            {
                var domain = filters.SelectedDomain.Trim().ToLower();
                query = query.Where(t => t.Domain.ToLower() == domain);
            }

            if (!string.IsNullOrWhiteSpace(filters.SkillFilter))
            {
                var skill = filters.SkillFilter.Trim().ToLower();
                query = query.Where(t => t.RequiredSkills.ToLower().Contains(skill));
            }

            if (!string.IsNullOrWhiteSpace(filters.SelectedProjectType))
            {
                var projectType = filters.SelectedProjectType.Trim().ToLower();
                query = query.Where(t => t.ProjectType.ToLower() == projectType);
            }

            if (filters.OnlyAvailable)
            {
                query = query.Where(t => t.IsAvailable && t.CurrentAssignedCount < t.Capacity);
            }

            var topics = await query
                .OrderByDescending(t => t.IsAvailable && t.CurrentAssignedCount < t.Capacity)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();

            filters.Results = topics.Select(MapCard).ToList();
            filters.AvailableDomains = await GetAvailableDomainsAsync();
            filters.ProjectTypes = ProjectTypes.ToList();

            return View(filters);
        }

        // GET: /Topics/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var topic = await _context.Topics
                .AsNoTracking()
                .Include(t => t.FacultyOwner)
                .FirstOrDefaultAsync(t => t.TopicId == id);

            if (topic == null)
            {
                return NotFound();
            }

            var model = new TopicDetailsViewModel
            {
                TopicId = topic.TopicId,
                Title = topic.Title,
                Description = topic.Description,
                Domain = topic.Domain,
                RequiredSkills = topic.RequiredSkills,
                ProjectType = topic.ProjectType,
                Capacity = topic.Capacity,
                CurrentAssignedCount = topic.CurrentAssignedCount,
                RemainingSlots = Math.Max(0, topic.Capacity - topic.CurrentAssignedCount),
                IsAvailable = topic.IsAvailable,
                IsOpenForApplications = topic.IsAvailable && topic.CurrentAssignedCount < topic.Capacity,
                FacultyOwnerId = topic.FacultyOwnerId,
                FacultyOwnerName = topic.FacultyOwner?.FullName ?? "Unassigned",
                FacultyOwnerEmail = topic.FacultyOwner?.Email ?? string.Empty,
                FacultyOwnerDesignation = topic.FacultyOwner?.Designation,
                FacultyOwnerDepartment = topic.FacultyOwner?.Department,
                FacultyOwnerResearchInterests = topic.FacultyOwner?.ResearchInterests,
                CreatedAt = topic.CreatedAt
            };

            return View(model);
        }

        // GET: /Topics/Create
        [HttpGet]
        [Authorize(Roles = "Supervisor,Coordinator")]
        public async Task<IActionResult> Create()
        {
            var model = new TopicFormViewModel();
            await PopulateFormOptionsAsync(model);
            return View(model);
        }

        // POST: /Topics/Create
        [HttpPost]
        [Authorize(Roles = "Supervisor,Coordinator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TopicFormViewModel model)
        {
            ValidateTopicForm(model);

            var currentUserId = _userManager.GetUserId(User);
            if (currentUserId == null)
            {
                return Challenge();
            }

            var ownerId = currentUserId;
            if (User.IsInRole("Coordinator") && !string.IsNullOrWhiteSpace(model.FacultyOwnerId))
            {
                var owner = await _userManager.FindByIdAsync(model.FacultyOwnerId);
                if (owner == null ||
                    (!await _userManager.IsInRoleAsync(owner, "Supervisor") &&
                     !await _userManager.IsInRoleAsync(owner, "Coordinator")))
                {
                    ModelState.AddModelError(nameof(model.FacultyOwnerId), "Please select a valid faculty owner.");
                }
                else
                {
                    ownerId = owner.Id;
                }
            }

            if (!ModelState.IsValid)
            {
                await PopulateFormOptionsAsync(model);
                return View(model);
            }

            var topic = new Topic
            {
                Title = model.Title.Trim(),
                Description = model.Description.Trim(),
                Domain = model.Domain,
                RequiredSkills = model.RequiredSkills.Trim(),
                ProjectType = model.ProjectType,
                Capacity = model.Capacity,
                CurrentAssignedCount = 0,
                IsAvailable = model.IsAvailable,
                FacultyOwnerId = ownerId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Topics.Add(topic);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Topic \"{topic.Title}\" was created successfully.";
            return RedirectToAction(nameof(Details), new { id = topic.TopicId });
        }

        // GET: /Topics/Edit/5
        [HttpGet]
        [Authorize(Roles = "Supervisor,Coordinator")]
        public async Task<IActionResult> Edit(int id)
        {
            var topic = await _context.Topics.AsNoTracking().FirstOrDefaultAsync(t => t.TopicId == id);
            if (topic == null)
            {
                return NotFound();
            }

            if (!CanManageTopic(topic))
            {
                return Forbid();
            }

            var model = new TopicFormViewModel
            {
                TopicId = topic.TopicId,
                Title = topic.Title,
                Description = topic.Description,
                Domain = topic.Domain,
                RequiredSkills = topic.RequiredSkills,
                ProjectType = topic.ProjectType,
                Capacity = topic.Capacity,
                IsAvailable = topic.IsAvailable,
                CurrentAssignedCount = topic.CurrentAssignedCount,
                FacultyOwnerId = topic.FacultyOwnerId
            };

            await PopulateFormOptionsAsync(model);
            model.FacultyOwnerDisplay = (await _userManager.FindByIdAsync(topic.FacultyOwnerId))?.FullName;
            return View(model);
        }

        // POST: /Topics/Edit/5
        [HttpPost]
        [Authorize(Roles = "Supervisor,Coordinator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TopicFormViewModel model)
        {
            var topic = await _context.Topics.FirstOrDefaultAsync(t => t.TopicId == id);
            if (topic == null)
            {
                return NotFound();
            }

            if (!CanManageTopic(topic))
            {
                return Forbid();
            }

            if (model.TopicId != 0 && model.TopicId != id)
            {
                return BadRequest();
            }

            ValidateTopicForm(model);

            if (model.Capacity < topic.CurrentAssignedCount)
            {
                ModelState.AddModelError(nameof(model.Capacity),
                    $"Capacity cannot be lower than the number of teams already assigned ({topic.CurrentAssignedCount}).");
            }

            if (User.IsInRole("Coordinator") && !string.IsNullOrWhiteSpace(model.FacultyOwnerId))
            {
                var owner = await _userManager.FindByIdAsync(model.FacultyOwnerId);
                if (owner == null ||
                    (!await _userManager.IsInRoleAsync(owner, "Supervisor") &&
                     !await _userManager.IsInRoleAsync(owner, "Coordinator")))
                {
                    ModelState.AddModelError(nameof(model.FacultyOwnerId), "Please select a valid faculty owner.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.CurrentAssignedCount = topic.CurrentAssignedCount;
                await PopulateFormOptionsAsync(model);
                model.FacultyOwnerDisplay = (await _userManager.FindByIdAsync(topic.FacultyOwnerId))?.FullName;
                return View(model);
            }

            topic.Title = model.Title.Trim();
            topic.Description = model.Description.Trim();
            topic.Domain = model.Domain;
            topic.RequiredSkills = model.RequiredSkills.Trim();
            topic.ProjectType = model.ProjectType;
            topic.Capacity = model.Capacity;
            topic.IsAvailable = model.IsAvailable;

            if (User.IsInRole("Coordinator") && !string.IsNullOrWhiteSpace(model.FacultyOwnerId))
            {
                topic.FacultyOwnerId = model.FacultyOwnerId;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Topic \"{topic.Title}\" was updated successfully.";
            return RedirectToAction(nameof(Details), new { id = topic.TopicId });
        }

        // POST: /Topics/ToggleAvailability/5
        [HttpPost]
        [Authorize(Roles = "Supervisor,Coordinator")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleAvailability(int id)
        {
            var topic = await _context.Topics.FirstOrDefaultAsync(t => t.TopicId == id);
            if (topic == null)
            {
                return NotFound();
            }

            if (!CanManageTopic(topic))
            {
                return Forbid();
            }

            topic.IsAvailable = !topic.IsAvailable;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = topic.IsAvailable
                ? $"Topic \"{topic.Title}\" is now open for applications."
                : $"Topic \"{topic.Title}\" has been closed for applications.";

            return RedirectToAction(nameof(Details), new { id = topic.TopicId });
        }

        private bool CanManageTopic(Topic topic)
        {
            var currentUserId = _userManager.GetUserId(User);
            return topic.FacultyOwnerId == currentUserId || User.IsInRole("Coordinator");
        }

        private void ValidateTopicForm(TopicFormViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Title))
            {
                ModelState.AddModelError(nameof(model.Title), "Topic title is required.");
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                ModelState.AddModelError(nameof(model.Description), "Description is required.");
            }

            if (string.IsNullOrWhiteSpace(model.RequiredSkills))
            {
                ModelState.AddModelError(nameof(model.RequiredSkills), "Required skills are required.");
            }

            if (!ProjectDomains.Contains(model.Domain, StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.Domain), "Please choose a valid domain from the list.");
            }

            if (!ProjectTypes.Contains(model.ProjectType, StringComparer.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.ProjectType), "Project type must be either Project or Thesis.");
            }
        }

        private async Task PopulateFormOptionsAsync(TopicFormViewModel model)
        {
            model.AvailableDomains = ProjectDomains.ToList();
            model.ProjectTypes = ProjectTypes.ToList();

            if (!User.IsInRole("Coordinator"))
            {
                return;
            }

            var currentUserId = _userManager.GetUserId(User);
            model.FacultyOwnerId ??= currentUserId;

            var options = (await _userManager.GetUsersInRoleAsync("Supervisor"))
                .OrderBy(s => s.FullName)
                .Select(s => new SelectListItem($"{s.FullName} ({s.Email})", s.Id))
                .ToList();

            if (!string.IsNullOrWhiteSpace(model.FacultyOwnerId) &&
                options.All(o => o.Value != model.FacultyOwnerId))
            {
                var owner = await _userManager.FindByIdAsync(model.FacultyOwnerId);
                if (owner != null)
                {
                    options.Insert(0, new SelectListItem($"{owner.FullName} ({owner.Email})", owner.Id));
                }
            }

            model.FacultyOptions = options;
        }

        private async Task<List<string>> GetAvailableDomainsAsync()
        {
            var storedDomains = await _context.Topics
                .AsNoTracking()
                .Select(t => t.Domain)
                .Distinct()
                .ToListAsync();

            return ProjectDomains
                .Union(storedDomains, StringComparer.OrdinalIgnoreCase)
                .OrderBy(d => d)
                .ToList();
        }

        private static TopicCardViewModel MapCard(Topic topic)
        {
            return new TopicCardViewModel
            {
                TopicId = topic.TopicId,
                Title = topic.Title,
                Description = topic.Description,
                Domain = topic.Domain,
                RequiredSkills = topic.RequiredSkills,
                ProjectType = topic.ProjectType,
                Capacity = topic.Capacity,
                CurrentAssignedCount = topic.CurrentAssignedCount,
                RemainingSlots = Math.Max(0, topic.Capacity - topic.CurrentAssignedCount),
                IsAvailable = topic.IsAvailable,
                IsOpenForApplications = topic.IsAvailable && topic.CurrentAssignedCount < topic.Capacity,
                FacultyOwnerName = topic.FacultyOwner?.FullName ?? "Unassigned",
                FacultyOwnerEmail = topic.FacultyOwner?.Email ?? string.Empty,
                CreatedAt = topic.CreatedAt
            };
        }
    }
}
