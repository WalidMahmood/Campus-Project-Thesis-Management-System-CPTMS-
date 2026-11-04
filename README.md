# Campus Project & Thesis Management System (CPTMS)

![.NET](https://img.shields.io/badge/.NET-8.0_LTS-512BD4?logo=dotnet)
![Architecture](https://img.shields.io/badge/Architecture-MVC_(Model--View--Controller)-success)
![Database](https://img.shields.io/badge/ORM-EF_Core_8-blue)
![Frontend](https://img.shields.io/badge/UI-Razor_Views_%2B_Bootstrap_5-purple)
![Course](https://img.shields.io/badge/Course-CSE470:_Software_Engineering-orange)
![Section](https://img.shields.io/badge/Section-13-informational)

**Campus Project & Thesis Management System (CPTMS)** is an academic workflow management system engineered for university departments. It coordinates the lifecycle of undergraduate course projects and theses—from team formation and eligibility checking, topic discovery, and supervisor matching, through milestone deliverable tracking, defense scheduling, and rubric-based evaluations.

---

## 👥 Course & Team Information

* **Course Code & Title:** CSE470: Software Engineering
* **Section:** 13
* **Section Faculty:** **Mr. Soumya Swagata Biswas [SOSB]** ([@soumya-swagata](https://github.com/soumya-swagata))

### Development Team
| Name | Student ID | GitHub Profile |
| :--- | :--- | :--- |
| **Walid Mahmood** | 22201456 | [@WalidMahmood](https://github.com/WalidMahmood) |
| **Sabab Al Saba** | 24101529 | [@Sabab90](https://github.com/Sabab90) |
| **Md Iftekhar Islam Abir** | 21301109 | — |

---

## 🏛️ System Architecture & MVC Structure

The system strictly adheres to the **Model-View-Controller (MVC)** architectural pattern as mandated by the course guidelines (No Python backend frameworks, no MERN stack).

In this unified ASP.NET Core MVC architecture:
* **Models (`/Models`)**: Represent application domain data, business logic, and database schemas via Entity Framework Core Code-First.
* **Views (`/Views`)**: Server-side Razor templates (`.cshtml`) paired with Bootstrap 5 and modern CSS for user presentation and interactions.
* **Controllers (`/Controllers`)**: Coordinate incoming HTTP requests, model validations, role-based authorization, and response view routing.

```
CPTMS/
│
├── Controllers/                 # CONTROLLER LAYER
│   ├── AccountController.cs    # Authentication (Login, Logout, Register, Access Control)
│   └── HomeController.cs       # Primary dashboard and navigation coordinator
│
├── Models/                      # MODEL LAYER
│   ├── Entities/               # Domain Entities & EF Core schemas
│   │   ├── ApplicationUser.cs  # Extended user identity (Student, Supervisor, Coordinator)
│   │   ├── ProjectTeam.cs      # Team entity & TeamMembership
│   │   └── Project.cs          # Project, Proposal, and Milestone entities
│   │
│   └── ViewModels/             # Strongly typed presentation models
│       ├── LoginViewModel.cs   # Login input validation & data annotations
│       └── RegisterViewModel.cs# Registration validation & role selection
│
├── Views/                       # VIEW LAYER (Server-Side Razor Templates)
│   ├── Account/
│   │   ├── Login.cshtml        # Responsive authentication interface
│   │   ├── Register.cshtml     # Institutional account registration
│   │   └── AccessDenied.cshtml # Unauthorized role error view
│   ├── Home/
│   │   ├── Index.cshtml        # Primary dashboard with role overview
│   │   └── Privacy.cshtml      # Architectural compliance & system info
│   └── Shared/
│       ├── _Layout.cshtml      # Global master layout with Bootstrap 5
│       ├── _LoginPartial.cshtml# Header authentication state & logout button
│       └── _ValidationScriptsPartial.cshtml
│
├── Data/                        # DATA ACCESS LAYER
│   └── ApplicationDbContext.cs # EF Core DbContext, relationships & automatic role seeding
│
├── docs/                        # SPECIFICATIONS
│   └── CPTMS_SRS_Draft.docx.pdf # Project Requirements Specification (SRS)
│
├── wwwroot/                     # STATIC ASSETS
│   ├── css/site.css            # Custom styling & responsive layouts
│   └── js/site.js              # Client-side scripts & notification handlers
│
├── Program.cs                   # Application startup, DI container, routing & middleware
├── appsettings.json             # Configuration & database connection strings
└── CPTMS.csproj                 # .NET 8.0 SDK project definition
```

---

## 🚀 Getting Started

### Prerequisites
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* Microsoft SQL Server / LocalDB (or SQLite out-of-the-box fallback)
* Modern web browser

### Running Locally
1. **Clone the repository:**
   ```bash
   git clone https://github.com/WalidMahmood/Campus-Project-Thesis-Management-System-CPTMS-.git
   cd Campus-Project-Thesis-Management-System-CPTMS-
   ```

2. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

3. **Run the application:**
   ```bash
   dotnet run
   ```

4. **Access the application:**
   Open your browser and navigate to:
   ```
   https://localhost:5001 or http://localhost:5000
   ```
   *(The database and demo accounts are automatically initialized and seeded on first startup).*

---

## 🔑 Demo Accounts for Evaluation

The login interface includes role-based accounts for testing:

| Role | Email | Password | Access / Purpose |
| :--- | :--- | :--- | :--- |
| **Department Coordinator** | `coordinator@cptms.edu` | `Admin@123456` | Faculty administrator (topic approval, defense scheduling) |
| **Faculty Supervisor** | `supervisor@cptms.edu` | `Supervisor@123456` | Thesis supervisor (proposal review, milestone tracking) |
| **Student** | `student@cptms.edu` | `Student@123456` | Student (team creation, proposal submission) |

---

## 📋 Sprint 0 Status & Attainment (Assignment 1)

* [x] **Framework Setup & Architecture:** Finalized technology stack (**ASP.NET Core MVC .NET 8 + EF Core 8 + Razor Views + Bootstrap 5**) ensuring complete compliance with course rules (No Python backend, No MERN stack).
* [x] **Repository & MVC Structure:** Created standard MVC project structure with clear separation across `Models/`, `Views/`, `Controllers/`, and `Data/`.
* [x] **Authentication Baseline:** Developed Login, Logout, and Registration interface with input validation and role-based access control.
* [x] **Data Modeling & Seed Configuration:** Implemented initial domain entities (`ApplicationUser`, `ProjectTeam`, `Project`, `Proposal`, `Milestone`) and automated database seeding in `ApplicationDbContext`.
* [x] **SRS Documentation:** Prepared and structured the project SRS in the repository `docs/` folder.
* [x] **Version Control:** Repository initialized with branch-based development workflow, `.gitignore`, and contributor configuration.
