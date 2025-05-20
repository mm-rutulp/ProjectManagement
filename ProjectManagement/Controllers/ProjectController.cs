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
                // Admins can see all projects
                var allProjects = await _context.Projects.ToListAsync();
                return View(allProjects);
            }
            else
            {
                // Regular employees see only their assigned projects
                var assignedProjects = await _context.ProjectAssignments
                    .Where(pa => pa.UserId == currentUser.Id)
                    .Include(pa => pa.Project)
                    .Select(pa => pa.Project)
                    .Distinct()
                    .ToListAsync();
                
                return View(assignedProjects);
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
                var hasAccess = await _context.ProjectAssignments
                    .AnyAsync(pa => pa.ProjectId == id && pa.UserId == currentUser.Id);
                
                if (!hasAccess)
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

            var project = await _context.Projects
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
            if (project != null)
            {
                _context.Projects.Remove(project);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Project/Assign/5
        [Authorize]
        public async Task<IActionResult> Assign(int? id)
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

            var currentUser = await _userManager.GetUserAsync(User);
            var userIsAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            // Check access for non-admin users
            if (!userIsAdmin)
            {
                var hasAccess = await _context.ProjectAssignments
                    .AnyAsync(pa => pa.ProjectId == id && pa.UserId == currentUser.Id) ||
                    await _context.ProjectShadowResourceAssignments
                    .AnyAsync(psa => psa.ProjectId == id && psa.ShadowResourceId == currentUser.Id);
                
                if (!hasAccess)
                {
                    return Forbid();
                }
            }

            // Get all employees
            var employees = await _userManager.GetUsersInRoleAsync("Employee");
            
            // Get current regular assignments for this project
            var currentAssignments = await _context.ProjectAssignments
                .Where(pa => pa.ProjectId == id)
                .Include(pa => pa.User)
                .ToListAsync();
            
            // Get current shadow resource assignments for this project
            var currentShadowAssignments = await _context.ProjectShadowResourceAssignments
                .Where(psa => psa.ProjectId == id)
                .Include(psa => psa.ShadowResource)
                .Include(psa => psa.ProjectOnBoardUser)
                .ToListAsync();

            // Filter assignments based on user role
            if (userIsAdmin)
            {
                // Admin only sees regular assignments
                currentShadowAssignments = new List<ProjectShadowResourceAssignment>();
            }
            else
            {
                // Regular employees only see their own shadow resources
                currentAssignments = new List<ProjectAssignment>();
                currentShadowAssignments = currentShadowAssignments
                    .Where(psa => psa.ProjectOnBoardUserId == currentUser.Id)
                    .ToList();
            }

            var viewModel = new ProjectAssignmentViewModel
            {
                ProjectId = id.Value,
                ProjectName = project.Name,
                AvailableEmployees = employees.ToList(),
                CurrentAssignments = currentAssignments,
                CurrentShadowAssignments = currentShadowAssignments
            };

            return View(viewModel);
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
                .FirstOrDefaultAsync(pa => pa.ProjectId == model.ProjectId && pa.UserId == model.UserId);

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

            // Check if the shadow resource is already assigned to this project
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