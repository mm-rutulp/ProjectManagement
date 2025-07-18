using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Data;
using ProjectManagement.Models;
using ProjectManagement.ViewModels;

namespace ProjectManagement.Controllers
{
    [Authorize]
    public class ProjectController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProjectController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: ProjectP
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var userIsAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (userIsAdmin)
            {
                // Admins can see all projects that are not marked as deleted
                var allProjects = await _context.Projects
                    .Where(p => !p.IsDeleted)
                    .ToListAsync();

                return View(allProjects);
            }
            else
            {
                // Regular users see projects where they are assigned directly or as a shadow resource
                var regularProjects = await _context.ProjectAssignments
                    .Where(pa => pa.UserId == currentUser.Id && !pa.IsDeleted && !pa.Project.IsDeleted)
                    .Include(pa => pa.Project)
                    .Select(pa => pa.Project)
                    .Distinct()
                    .ToListAsync();

                var shadowProjects = await _context.ProjectShadowResourceAssignments
                    .Where(psa => psa.ShadowResourceId == currentUser.Id && !psa.IsDeleted && !psa.Project.IsDeleted)
                    .Include(psa => psa.Project)
                    .Select(psa => psa.Project)
                    .Distinct()
                    .ToListAsync();

                var allAssignedProjects = regularProjects
                    .Union(shadowProjects, new ProjectComparer()) // Use comparer to avoid duplicates
                    .OrderBy(p => p.Name)
                    .ToList();

                return View(allAssignedProjects);
            }
        }

        // GET: Project/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects
                .Where(p => !p.IsDeleted)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (project == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var userIsAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            // Check access if not admin
            if (!userIsAdmin)
            {
                var hasRegularAccess = await _context.ProjectAssignments
                    .AnyAsync(pa => pa.ProjectId == id && pa.UserId == currentUser.Id && !pa.IsDeleted);

                var isShadowResource = await _context.ProjectShadowResourceAssignments
                    .AnyAsync(psa => psa.ProjectId == id && psa.ShadowResourceId == currentUser.Id && !psa.IsDeleted);

                if (!hasRegularAccess && !isShadowResource)
                {
                    return Forbid();
                }
            }


            return View(project);
        }

        // GET: Project/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Project/Create
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Project project)
        {
            if (ModelState.IsValid)
            {
                _context.Add(project);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(project);
        }

        // GET: Project/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var project = await _context.Projects.FindAsync(id);
            if (project == null)
            {
                return NotFound();
            }
            return View(project);
        }

        // POST: Project/Edit/5
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Project project)
        {
            if (id != project.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(project);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProjectExists(project.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(project);
        }

        // GET: Project/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Only fetch projects that are NOT soft-deleted
            var project = await _context.Projects
                .Where(p => !p.IsDeleted)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }
        // POST: Project/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var project = await _context.Projects.FindAsync(id);

            if (project != null && !project.IsDeleted)
            {
                
                project.IsDeleted = true;
                _context.Projects.Update(project);

                
                var projectAssignments = await _context.ProjectAssignments
                    .Where(pa => pa.ProjectId == id && !pa.IsDeleted)
                    .ToListAsync();

                foreach (var assignment in projectAssignments)
                {
                    assignment.IsDeleted = true;
                    _context.ProjectAssignments.Update(assignment);
                }

               
                var shadowAssignments = await _context.ProjectShadowResourceAssignments
                    .Where(psa => psa.ProjectId == id && !psa.IsDeleted)
                    .ToListAsync();

                foreach (var shadowAssignment in shadowAssignments)
                {
                    shadowAssignment.IsDeleted = true;
                    _context.ProjectShadowResourceAssignments.Update(shadowAssignment);
                }

                await _context.SaveChangesAsync();
            }


            return RedirectToAction(nameof(Index));
        }

        // GET: Project/Assign/5
        [Authorize]
        public async Task<IActionResult> Assign(int? id)
        {
            if (id == null) return NotFound();

            var project = await _context.Projects.FindAsync(id);
            if (project == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            var employees = new List<ApplicationUser>();
            var currentAssignments = new List<ProjectAssignment>();
            var currentShadowAssignments = new List<ProjectShadowResourceAssignment>();
            var unifiedTeam = new List<ProjectTeamMemberViewModel>();

            if (isAdmin)
            {
                employees = (await _userManager.GetUsersInRoleAsync("Employee")).Where(u => !u.IsDeleted).ToList();
                currentAssignments = await _context.ProjectAssignments
                    .Where(pa => pa.ProjectId == id && !pa.IsDeleted)
                    .Include(pa => pa.User)
                    .ToListAsync();

                currentShadowAssignments = await _context.ProjectShadowResourceAssignments
                    .Where(psa => psa.ProjectId == id)
                    .Include(psa => psa.ShadowResource)
                    .Include(psa => psa.ProjectOnBoardUser)
                    .ToListAsync();
            }
            else
            {
                employees = (await _userManager.GetUsersInRoleAsync("Employee")).Where(u => !u.IsDeleted).ToList();
                var hasAccess = await _context.ProjectAssignments
                    .AnyAsync(pa => pa.ProjectId == id && pa.UserId == currentUser.Id && !pa.IsDeleted);

                var isShadow = await _context.ProjectShadowResourceAssignments
                    .AnyAsync(psa => psa.ProjectId == id && psa.ShadowResourceId == currentUser.Id && !psa.IsDeleted);
                ViewBag.isShadow = isShadow;

                if (!hasAccess && !isShadow) return Forbid();

                if (hasAccess)
                {
                    currentShadowAssignments = await _context.ProjectShadowResourceAssignments
                        .Where(psa => psa.ProjectId == id)
                        .Include(psa => psa.ShadowResource)
                        .Include(psa => psa.ProjectOnBoardUser)
                        .ToListAsync();
                }
                else
                {
                    currentShadowAssignments = await _context.ProjectShadowResourceAssignments
                        .Where(psa => psa.ProjectId == id)
                        .Include(psa => psa.ShadowResource)
                        .Include(psa => psa.ProjectOnBoardUser)
                        .ToListAsync();
                }   

                if (!hasAccess && isShadow)
                {
                    var shadowAssignment = currentShadowAssignments.FirstOrDefault(psa => psa.ShadowResourceId == currentUser.Id);

                    unifiedTeam.Add(new ProjectTeamMemberViewModel
                    {
                        UserId = currentUser.Id,
                        FullName = currentUser.FullName,
                        Role = "Shadow Resource",
                        IsShadow = true,
                        AddedByUserId = shadowAssignment?.ProjectOnBoardUserId,
                        AddedByFullName = shadowAssignment?.ProjectOnBoardUser?.FullName ?? "Unknown"
                    });
                }
            }

            if (!unifiedTeam.Any())
            {
                unifiedTeam.AddRange(currentAssignments.Select(a => new ProjectTeamMemberViewModel
                {
                    UserId = a.UserId,
                    FullName = a.User.FullName,
                    Role = a.Role,
                    IsShadow = false
                }));

                unifiedTeam.AddRange(currentShadowAssignments
                    .Where(sa => !unifiedTeam.Any(ut => ut.UserId == sa.ShadowResourceId))
                    .Select(sa => new ProjectTeamMemberViewModel
                    {
                        UserId = sa.ShadowResourceId,
                        FullName = sa.ShadowResource.FullName,
                        Role = "Shadow Resource",
                        IsShadow = true,
                        AddedByUserId = sa.ProjectOnBoardUserId,
                        AddedByFullName = string.IsNullOrEmpty(sa.ProjectOnBoardUser?.FullName) ? "Unknown" : sa.ProjectOnBoardUser.FullName


                    }));
            }

            return View("Assign", new ProjectAssignmentViewModel
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                AvailableEmployees = employees,
                CurrentAssignments = currentAssignments,
                CurrentShadowAssignments = currentShadowAssignments,
                UnifiedTeamMembers = unifiedTeam
            });
        }


        // POST: Project/AssignEmployee
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignEmployee(ProjectAssignmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Assign), new { id = model.ProjectId });
            }

            // Check if the user is already assigned to this project
            var existingAssignment = await _context.ProjectAssignments
                .FirstOrDefaultAsync(pa => pa.ProjectId == model.ProjectId && pa.UserId == model.UserId && !pa.IsDeleted);

            if (existingAssignment == null)
            {
                var assignment = new ProjectAssignment
                {
                    ProjectId = model.ProjectId,
                    UserId = model.UserId,
                    Role = model.Role,
                    AssignedDate = DateTime.Now
                };

                _context.Add(assignment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Assign), new { id = model.ProjectId });
        }

        // POST: Project/AssignShadowResource
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> AssignShadowResource(ProjectAssignmentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Assign), new { id = model.ProjectId });
            }

            var currentUser = await _userManager.GetUserAsync(User);

            // Prevent assigning shadow role to someone already a regular developer
            var isAlreadyRegularDeveloper = await _context.ProjectAssignments
                .AnyAsync(pa => pa.ProjectId == model.ProjectId && pa.UserId == model.UserId && !pa.IsDeleted);

            if (isAlreadyRegularDeveloper)
            {
                TempData["Error"] = "This user is already assigned as a regular developer.";
                return RedirectToAction(nameof(Assign), new { id = model.ProjectId });
            }

            //  Prevent duplicate shadow assignment
            var existingShadowAssignment = await _context.ProjectShadowResourceAssignments
                .FirstOrDefaultAsync(psa => psa.ProjectId == model.ProjectId && psa.ShadowResourceId == model.UserId);

            if (existingShadowAssignment == null)
            {
                var shadowAssignment = new ProjectShadowResourceAssignment
                {
                    ProjectId = model.ProjectId,
                    ShadowResourceId = model.UserId,
                    Role = model.Role,
                    AssignedDate = DateTime.Now,
                    ProjectOnBoardUserId = currentUser.Id
                };

                _context.Add(shadowAssignment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Assign), new { id = model.ProjectId });
        }

        // POST: Project/RemoveAssignment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveAssignment(int id)
        {
            var assignment = await _context.ProjectAssignments.FindAsync(id);
            if (assignment == null)
            {
                return NotFound();
            }

            int projectId = assignment.ProjectId;
            _context.ProjectAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Assign), new { id = projectId });
        }



        //POST : Project/RemoceShadowResource 
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> RemoveShadowResource(string shadowResourceId, int projectId)
        {
            var shadowAssignment = await _context.ProjectShadowResourceAssignments
                .FirstOrDefaultAsync(sa => sa.ProjectId == projectId && sa.ShadowResourceId == shadowResourceId);

            if (shadowAssignment == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var userIsAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            // Admins can remove any assignment, employees can only remove their own shadow resources
            if (!userIsAdmin && shadowAssignment.ProjectOnBoardUserId != currentUser.Id)
            {
                return Forbid();
            }

            _context.ProjectShadowResourceAssignments.Remove(shadowAssignment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Assign), new { id = projectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveTeamMember(string userId, int projectId)
        {
            var assignment = await _context.ProjectAssignments
                .FirstOrDefaultAsync(pa => pa.ProjectId == projectId && pa.UserId == userId && !pa.IsDeleted);

            if (assignment == null)
            {
                return NotFound();
            }

            assignment.IsDeleted = true;
            _context.ProjectAssignments.Update(assignment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Assign), new { id = projectId });
        }

        // POST: Project/RemoveShadowAssignment/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> RemoveShadowAssignment(int id)
        {
            var shadowAssignment = await _context.ProjectShadowResourceAssignments.FindAsync(id);
            if (shadowAssignment == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var userIsAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            // Admins can remove any assignment, employees can only remove their own shadow resources
            if (!userIsAdmin && (shadowAssignment.ProjectOnBoardUserId != currentUser.Id))
            {
                return Forbid();
            }

            int projectId = shadowAssignment.ProjectId;
            _context.ProjectShadowResourceAssignments.Remove(shadowAssignment);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Assign), new { id = projectId });
        }

        // API: Project/ClearTestData
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        [Route("api/[controller]/ClearTestData")]
        public async Task<IActionResult> ClearTestData()
        {
            try
            {
                // Clear monthly summaries (depends on projects, users, worklogs)
                var monthlySummaries = await _context.MonthlySummaries.ToListAsync();
                _context.MonthlySummaries.RemoveRange(monthlySummaries);
                
                // Clear worklogs (depends on projects and users)
                var worklogs = await _context.Worklogs.ToListAsync();
                _context.Worklogs.RemoveRange(worklogs);
                
                // Clear shadow resource assignments (depends on projects and users)
                var shadowResourceAssignments = await _context.ProjectShadowResourceAssignments.ToListAsync();
                _context.ProjectShadowResourceAssignments.RemoveRange(shadowResourceAssignments);
                
                // Clear regular project assignments (depends on projects and users)
                var projectAssignments = await _context.ProjectAssignments.ToListAsync();
                _context.ProjectAssignments.RemoveRange(projectAssignments);
                
                // Clear projects (must be removed last since other entities depend on them)
                var projects = await _context.Projects.ToListAsync();
                _context.Projects.RemoveRange(projects);
                
                // Save changes to database
                await _context.SaveChangesAsync();
                
                return Ok(new { success = true, message = "All project-related test data has been cleared successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Error clearing test data: {ex.Message}" });
            }
        }

        private bool ProjectExists(int id)
        {
            return _context.Projects.Any(e => e.Id == id);
        }
    }
} 