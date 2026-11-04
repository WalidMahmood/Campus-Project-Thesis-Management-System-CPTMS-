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

            string[] roles = { "Student", "Supervisor", "Evaluator", "Coordinator", "Administrator" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed Demo Coordinator (Faculty Admin)
            var coordinatorEmail = "coordinator@cptms.edu";
            var coordinatorUser = await userManager.FindByEmailAsync(coordinatorEmail);
            if (coordinatorUser == null)
            {
                coordinatorUser = new ApplicationUser
                {
                    UserName = coordinatorEmail,
                    Email = coordinatorEmail,
                    FullName = "Mr. Soumya Swagata Biswas",
                    Department = "Computer Science and Engineering",
                    Designation = "Assistant Professor & Coordinator",
                    UserRole = "Coordinator",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(coordinatorUser, "Admin@123456");
                await userManager.AddToRoleAsync(coordinatorUser, "Coordinator");
            }

            // Seed Demo Student
            var studentEmail = "student@cptms.edu";
            var studentUser = await userManager.FindByEmailAsync(studentEmail);
            if (studentUser == null)
            {
                studentUser = new ApplicationUser
                {
                    UserName = studentEmail,
                    Email = studentEmail,
                    FullName = "Walid Mahmood",
                    Department = "Computer Science and Engineering",
                    StudentIdNumber = "22201456",
                    CGPA = 3.85,
                    CompletedCredits = 110,
                    UserRole = "Student",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(studentUser, "Student@123456");
                await userManager.AddToRoleAsync(studentUser, "Student");
            }

            // Seed Demo Supervisor
            var supervisorEmail = "supervisor@cptms.edu";
            var supervisorUser = await userManager.FindByEmailAsync(supervisorEmail);
            if (supervisorUser == null)
            {
                supervisorUser = new ApplicationUser
                {
                    UserName = supervisorEmail,
                    Email = supervisorEmail,
                    FullName = "Dr. Academic Supervisor",
                    Department = "Computer Science and Engineering",
                    Designation = "Associate Professor",
                    MaxSupervisionCapacity = 5,
                    ResearchInterests = "Software Engineering, Distributed Systems, ML",
                    UserRole = "Supervisor",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(supervisorUser, "Supervisor@123456");
                await userManager.AddToRoleAsync(supervisorUser, "Supervisor");
            }
        }
    }
}
