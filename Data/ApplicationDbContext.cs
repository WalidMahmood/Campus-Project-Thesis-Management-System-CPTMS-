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
        public DbSet<EligibilityRule> EligibilityRules { get; set; } = null!;
        public DbSet<Topic> Topics { get; set; } = null!;
        public DbSet<FacultyProfile> FacultyProfiles { get; set; } = null!;

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

            // Configure Topic - FacultyOwner relation
            builder.Entity<Topic>()
                .HasOne(t => t.FacultyOwner)
                .WithMany()
                .HasForeignKey(t => t.FacultyOwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure FacultyProfile - User relation (one profile per faculty member)
            builder.Entity<FacultyProfile>()
                .HasIndex(fp => fp.UserId)
                .IsUnique();

            builder.Entity<FacultyProfile>()
                .HasOne(fp => fp.User)
                .WithMany()
                .HasForeignKey(fp => fp.UserId)
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

            await SeedUserAsync(new ApplicationUser
            {
                UserName = "student3@cptms.edu",
                Email = "student3@cptms.edu",
                FullName = "Md Iftekhar Islam Abir",
                Department = "Computer Science and Engineering",
                StudentIdNumber = "21301109",
                CGPA = 3.55,
                Semester = 11,
                CompletedCredits = 118,
                UserRole = "Student",
                EmailConfirmed = true
            }, "Student@123456", "Student");

            // Seed Demo Ineligible Student (useful for testing F5 eligibility rejections)
            await SeedUserAsync(new ApplicationUser
            {
                UserName = "ineligible@cptms.edu",
                Email = "ineligible@cptms.edu",
                FullName = "Junior Undergrad Test Student",
                Department = "Computer Science and Engineering",
                StudentIdNumber = "24301999",
                CGPA = 2.20,
                Semester = 5,
                CompletedCredits = 54,
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

            // Seed Default Eligibility Rules (F5)
            var seedContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
            if (!await seedContext.EligibilityRules.AnyAsync())
            {
                seedContext.EligibilityRules.AddRange(
                    new EligibilityRule
                    {
                        Name = "CSE Course Project Standard Eligibility",
                        ProjectType = "Project",
                        Department = "Computer Science and Engineering",
                        MinTeamSize = 2,
                        MaxTeamSize = 4,
                        MinSemester = 7,
                        MinCgpa = 2.50,
                        MinCompletedCredits = 75,
                        RequireSameDepartment = true,
                        IsActive = true,
                        UpdatedAt = DateTime.UtcNow
                    },
                    new EligibilityRule
                    {
                        Name = "CSE Undergraduate Thesis Eligibility",
                        ProjectType = "Thesis",
                        Department = "Computer Science and Engineering",
                        MinTeamSize = 1,
                        MaxTeamSize = 4,
                        MinSemester = 9,
                        MinCgpa = 3.00,
                        MinCompletedCredits = 95,
                        RequireSameDepartment = true,
                        IsActive = true,
                        UpdatedAt = DateTime.UtcNow
                    }
                );
                await seedContext.SaveChangesAsync();
            }

            // Seed Sample Topics (F2 - Topic Discovery)
            if (!await seedContext.Topics.AnyAsync())
            {
                var supervisor1 = await userManager.FindByEmailAsync("supervisor@cptms.edu");
                var supervisor2 = await userManager.FindByEmailAsync("supervisor2@cptms.edu");

                if (supervisor1 != null && supervisor2 != null)
                {
                    seedContext.Topics.AddRange(
                        new Topic
                        {
                            Title = "AI-Powered Crop Disease Detection",
                            Description = "Design and evaluate a convolutional neural network pipeline that identifies common crop diseases from leaf images and provides treatment guidance to small-scale farmers through a lightweight web dashboard.",
                            Domain = "Artificial Intelligence",
                            RequiredSkills = "Python, TensorFlow, OpenCV, Flask",
                            ProjectType = "Project",
                            Capacity = 3,
                            CurrentAssignedCount = 1,
                            IsAvailable = true,
                            FacultyOwnerId = supervisor2.Id
                        },
                        new Topic
                        {
                            Title = "Zero-Trust Session Management for University Portals",
                            Description = "Investigate zero-trust authentication patterns and prototype a session-management layer that continuously validates device posture, user behaviour, and least-privilege access for a campus portal.",
                            Domain = "Cybersecurity",
                            RequiredSkills = "C#, ASP.NET Core Identity, OAuth 2.0, SQL",
                            ProjectType = "Thesis",
                            Capacity = 2,
                            CurrentAssignedCount = 0,
                            IsAvailable = true,
                            FacultyOwnerId = supervisor1.Id
                        },
                        new Topic
                        {
                            Title = "Explainable Clinical Decision Support for Dengue Triage",
                            Description = "Build and explain a machine-learning triage model for dengue patients using tabular clinical data, with SHAP-based explanations reviewed against physician judgement.",
                            Domain = "Data Science",
                            RequiredSkills = "Python, scikit-learn, SHAP, Pandas",
                            ProjectType = "Thesis",
                            Capacity = 1,
                            CurrentAssignedCount = 1,
                            IsAvailable = true,
                            FacultyOwnerId = supervisor2.Id
                        },
                        new Topic
                        {
                            Title = "Smart Campus Navigation and Room Booking App",
                            Description = "Develop a mobile-first campus companion that combines indoor navigation, live room occupancy, and booking workflows for students and faculty.",
                            Domain = "Web & Distributed Systems",
                            RequiredSkills = "Flutter, REST APIs, ASP.NET Core, SQLite",
                            ProjectType = "Project",
                            Capacity = 4,
                            CurrentAssignedCount = 2,
                            IsAvailable = true,
                            FacultyOwnerId = supervisor1.Id
                        },
                        new Topic
                        {
                            Title = "Blockchain-Based Academic Credential Verification",
                            Description = "Examine decentralized identity standards and prototype a verifiable-credential registry that allows employers to validate university certificates without contacting the registrar.",
                            Domain = "Cybersecurity",
                            RequiredSkills = "Solidity, Ethereum, Web3.js, Cryptography",
                            ProjectType = "Thesis",
                            Capacity = 2,
                            CurrentAssignedCount = 0,
                            IsAvailable = false,
                            FacultyOwnerId = supervisor2.Id
                        },
                        new Topic
                        {
                            Title = "Distributed Log Analytics Dashboard for Campus Services",
                            Description = "Create an observability dashboard that ingests application logs from multiple campus services, detects anomalies, and visualises service health for the IT operations team.",
                            Domain = "Software Engineering",
                            RequiredSkills = "C#, EF Core, SQL Server, Grafana, Docker",
                            ProjectType = "Project",
                            Capacity = 3,
                            CurrentAssignedCount = 1,
                            IsAvailable = true,
                            FacultyOwnerId = supervisor1.Id
                        });

                    await seedContext.SaveChangesAsync();
                }
            }

            // Seed Faculty Profiles (F3 - Faculty Supervision Capacity & Interests)
            if (!await seedContext.FacultyProfiles.AnyAsync())
            {
                async Task SeedFacultyProfileAsync(string email, int declaredLoad, int approvedLimit, int currentLoad, string eligibility)
                {
                    var user = await userManager.FindByEmailAsync(email);
                    if (user == null) return;
                    if (await seedContext.FacultyProfiles.AnyAsync(p => p.UserId == user.Id)) return;

                    seedContext.FacultyProfiles.Add(new FacultyProfile
                    {
                        UserId = user.Id,
                        Department = user.Department,
                        ResearchInterests = string.IsNullOrWhiteSpace(user.ResearchInterests)
                            ? "Not specified"
                            : user.ResearchInterests,
                        EligibilityConditions = eligibility,
                        MaxSupervisionLoad = declaredLoad,
                        ApprovedCapacityLimit = approvedLimit,
                        CurrentSupervisionLoad = currentLoad,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                await SeedFacultyProfileAsync("supervisor@cptms.edu", 4, 5, 1,
                    "Minimum CGPA 3.2; completed CSE370 and CSE470.");
                await SeedFacultyProfileAsync("supervisor2@cptms.edu", 3, 4, 0,
                    "Minimum CGPA 3.0; strong Python and data-analysis background.");

                await seedContext.SaveChangesAsync();
            }
        }
    }
}
