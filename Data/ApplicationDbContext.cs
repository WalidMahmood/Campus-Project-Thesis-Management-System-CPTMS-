using CPTMS.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CPTMS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ProjectTeam> ProjectTeams { get; set; } = null!;
        public DbSet<TeamMembership> TeamMemberships { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<Proposal> Proposals { get; set; } = null!;
        public DbSet<Milestone> Milestones { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure Team - Memberships relation
            builder.Entity<TeamMembership>()
                .HasOne(tm => tm.Team)
                .WithMany(t => t.Members)
                .HasForeignKey(tm => tm.TeamId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Team - Projects relation
            builder.Entity<Project>()
                .HasOne(p => p.Team)
                .WithMany(t => t.Projects)
                .HasForeignKey(p => p.TeamId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure Project - Proposals relation
            builder.Entity<Proposal>()
                .HasOne(pr => pr.Project)
                .WithMany(p => p.Proposals)
                .HasForeignKey(pr => pr.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Project - Milestones relation
            builder.Entity<Milestone>()
                .HasOne(m => m.Project)
                .WithMany(p => p.Milestones)
                .HasForeignKey(m => m.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var logger = serviceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

            string[] roles = { "Student", "Supervisor", "Evaluator", "Coordinator", "Administrator" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
                    if (!roleResult.Succeeded)
                    {
                        logger.LogWarning("Failed to create role {Role}: {Errors}", role, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    }
                }
            }

            async Task SeedUserAsync(ApplicationUser newUser, string password, string roleName)
            {
                var existing = await userManager.FindByEmailAsync(newUser.Email!);
                if (existing != null) return;

                var createResult = await userManager.CreateAsync(newUser, password);
                if (!createResult.Succeeded)
                {
                    logger.LogWarning("Failed to seed user {Email}: {Errors}", newUser.Email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    return;
                }

                var addRoleResult = await userManager.AddToRoleAsync(newUser, roleName);
                if (!addRoleResult.Succeeded)
                {
                    logger.LogWarning("Failed to assign role {Role} to {Email}: {Errors}", roleName, newUser.Email, string.Join(", ", addRoleResult.Errors.Select(e => e.Description)));
                }
            }

            // Seed Demo Coordinator (Faculty Admin)
            await SeedUserAsync(new ApplicationUser
            {
                UserName = "coordinator@cptms.edu",
                Email = "coordinator@cptms.edu",
                FullName = "Mr. Soumya Swagata Biswas",
                Department = "Computer Science and Engineering",
                Designation = "Assistant Professor & Coordinator",
                UserRole = "Coordinator",
                EmailConfirmed = true
            }, "Admin@123456", "Coordinator");

            // Seed Demo Students
            await SeedUserAsync(new ApplicationUser
            {
                UserName = "student@cptms.edu",
                Email = "student@cptms.edu",
                FullName = "Walid Mahmood",
                Department = "Computer Science and Engineering",
                StudentIdNumber = "22201456",
                CGPA = 3.85,
                Semester = 10,
                CompletedCredits = 110,
                UserRole = "Student",
                EmailConfirmed = true
            }, "Student@123456", "Student");

            await SeedUserAsync(new ApplicationUser
            {
                UserName = "student2@cptms.edu",
                Email = "student2@cptms.edu",
                FullName = "Sabab Al Saba",
                Department = "Computer Science and Engineering",
                StudentIdNumber = "24101529",
                CGPA = 3.70,
                Semester = 9,
                CompletedCredits = 98,
                UserRole = "Student",
                EmailConfirmed = true
            }, "Student@123456", "Student");

            // Seed Demo Supervisors
            await SeedUserAsync(new ApplicationUser
            {
                UserName = "supervisor@cptms.edu",
                Email = "supervisor@cptms.edu",
                FullName = "Dr. Academic Supervisor",
                Department = "Computer Science and Engineering",
                Designation = "Associate Professor",
                MaxSupervisionCapacity = 5,
                ResearchInterests = "Software Engineering, Distributed Systems, ML",
                UserRole = "Supervisor",
                EmailConfirmed = true
            }, "Supervisor@123456", "Supervisor");

            await SeedUserAsync(new ApplicationUser
            {
                UserName = "supervisor2@cptms.edu",
                Email = "supervisor2@cptms.edu",
                FullName = "Dr. Nadia Rahman",
                Department = "Computer Science and Engineering",
                Designation = "Assistant Professor",
                MaxSupervisionCapacity = 4,
                ResearchInterests = "Computer Vision, NLP, Healthcare AI",
                UserRole = "Supervisor",
                EmailConfirmed = true
            }, "Supervisor@123456", "Supervisor");
        }
    }
}
