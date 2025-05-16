using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Data;
using ProjectManagement.Models;
using ProjectManagement.ViewModels;
using System.Data;

namespace ProjectManagement.Controllers
{
    [Authorize]
    public class WorklogController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WorklogController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Worklog
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var userWorklogs = await _context.Worklogs
                .Where(w => w.UserId == currentUser.Id)
                .Include(w => w.Project)
                .OrderByDescending(w => w.Date)
                .ToListAsync();

            return View(userWorklogs);
        }

        // GET: Worklog/Project/5
        public async Task<IActionResult> Project(int? id, string? userId, DateTime? startDate, DateTime? endDate)
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
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            // Check if non-admin user has access to this project
            if (!isAdmin)
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

            // Get only regular team members for this project (excluding shadow resources)
            var projectMembers = await _context.ProjectAssignments
                .Where(pa => pa.ProjectId == id)
                .Include(pa => pa.User)
                .Select(pa => pa.User)
                .Distinct()
                .ToListAsync();

            // Build the query for worklogs with filters
            var worklogsQuery = _context.Worklogs
                .Where(w => w.ProjectId == id);

            // Apply filters if provided
            if (!string.IsNullOrEmpty(userId))
            {
                worklogsQuery = worklogsQuery.Where(w => w.UserId == userId);
            }

            if (startDate.HasValue)
            {
                worklogsQuery = worklogsQuery.Where(w => w.Date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                worklogsQuery = worklogsQuery.Where(w => w.Date <= endDate.Value);
            }

            // Get the final results with includes
            var worklogs = await worklogsQuery
                .Include(w => w.User)
                .OrderByDescending(w => w.Date)
                .ToListAsync();

            var viewModel = new ProjectWorklogViewModel
            {
                ProjectId = id.Value,
                ProjectName = project.Name,
                UserId = userId,
                StartDate = startDate,
                EndDate = endDate,
                Worklogs = worklogs,
                ProjectMembers = projectMembers
            };

            return View(viewModel);
        }

        // GET: Worklog/Create/5
        public async Task<IActionResult> Create(int? id)
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
            
            // Check if user has access to this project
            var hasAccess = await _context.ProjectAssignments
                .AnyAsync(pa => pa.ProjectId == id && pa.UserId == currentUser.Id) ||
                await _context.ProjectShadowResourceAssignments
                .AnyAsync(psa => psa.ProjectId == id && psa.ShadowResourceId == currentUser.Id);

            if (!hasAccess)
            {
                return Forbid();
            }

            // Get all users who have added the current user as a shadow resource for this project
            var shadowResourceAssignments = await _context.ProjectShadowResourceAssignments
                .Where(psa => psa.ProjectId == id && psa.ShadowResourceId == currentUser.Id)
                .Include(psa => psa.ProjectOnBoardUser)
                .ToListAsync();

            var viewModel = new WorklogCreateViewModel
            {
                ProjectId = id.Value,
                ProjectName = project.Name,
                Date = DateTime.Today,
                UserId = currentUser.Id // Default to current user
            };

            // If the user is a shadow resource for this project, populate the dropdown with users who added them
            if (shadowResourceAssignments.Any())
            {
                viewModel.AvailableUsers = shadowResourceAssignments
                    .Select(psa => psa.ProjectOnBoardUser)
                    .Distinct()
                    .ToList();
            }
            else
            {
                // If not a shadow resource, only show the current user
                viewModel.AvailableUsers = new List<ApplicationUser> { currentUser };
            }

            return View(viewModel);
        }

        // POST: Worklog/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WorklogCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                
                // Check if the user has access to this project
                var hasAccess = await _context.ProjectAssignments
                    .AnyAsync(pa => pa.ProjectId == model.ProjectId && pa.UserId == currentUser.Id) ||
                    await _context.ProjectShadowResourceAssignments
                    .AnyAsync(psa => psa.ProjectId == model.ProjectId && psa.ShadowResourceId == currentUser.Id);

                if (!hasAccess)
                {
                    return Forbid();
                }

                // Verify that the selected user is valid
                var isValidUser = await _context.ProjectShadowResourceAssignments
                    .AnyAsync(psa => psa.ProjectId == model.ProjectId && 
                                   psa.ShadowResourceId == currentUser.Id && 
                                   psa.ProjectOnBoardUserId == model.UserId);

                if (!isValidUser && model.UserId != currentUser.Id)
                {
                    ModelState.AddModelError("UserId", "Invalid user selection");
                    return View(model);
                }

                var worklog = new Worklog
                {
                    ProjectId = model.ProjectId,
                    UserId = model.UserId,
                    Date = model.Date,
                    HoursWorked = model.HoursWorked,
                    Description = model.Description,
                    TaskType = model.TaskType,
                    CreatedAt = DateTime.Now,
                    IsShadowResourceWorklog = model.UserId != currentUser.Id,
                    ShadowResourceId = model.UserId != currentUser.Id ? currentUser.Id : null
                };

                _context.Add(worklog);
                await _context.SaveChangesAsync();
                
                return RedirectToAction(nameof(Project), new { id = model.ProjectId });
            }
            
            return View(model);
        }

        // GET: Worklog/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var worklog = await _context.Worklogs
                .Include(w => w.Project)
                .FirstOrDefaultAsync(w => w.Id == id);
            
            if (worklog == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            
            // Only the owner or admin can edit a worklog
            if (!isAdmin && worklog.UserId != currentUser.Id)
            {
                return Forbid();
            }

            var viewModel = new WorklogCreateViewModel
            {
                ProjectId = worklog.ProjectId,
                ProjectName = worklog.Project.Name,
                Date = worklog.Date,
                HoursWorked = worklog.HoursWorked,
                Description = worklog.Description,
                TaskType = worklog.TaskType
            };

            return View(viewModel);
        }

        // POST: Worklog/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, WorklogCreateViewModel model)
        {
            var worklog = await _context.Worklogs.FindAsync(id);
            if (worklog == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            
            // Only the owner or admin can edit a worklog
            if (!isAdmin && worklog.UserId != currentUser.Id)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                worklog.Date = model.Date;
                worklog.HoursWorked = model.HoursWorked;
                worklog.Description = model.Description;
                worklog.TaskType = model.TaskType;
                worklog.UpdatedAt = DateTime.Now;

                _context.Update(worklog);
                await _context.SaveChangesAsync();
                
                return RedirectToAction(nameof(Project), new { id = worklog.ProjectId });
            }
            
            return View(model);
        }

        // GET: Worklog/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var worklog = await _context.Worklogs
                .Include(w => w.Project)
                .Include(w => w.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (worklog == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            
            // Only the owner or admin can delete a worklog
            if (!isAdmin && worklog.UserId != currentUser.Id)
            {
                return Forbid();
            }

            return View(worklog);
        }

        // POST: Worklog/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var worklog = await _context.Worklogs.FindAsync(id);
            if (worklog == null)
            {
                return NotFound();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            
            // Only the owner or admin can delete a worklog
            if (!isAdmin && worklog.UserId != currentUser.Id)
            {
                return Forbid();
            }

            int projectId = worklog.ProjectId;
            _context.Worklogs.Remove(worklog);
            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(Project), new { id = projectId });
        }

        // GET: Worklog/MonthlySummary/5
        public async Task<IActionResult> MonthlySummary(int? id)
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
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            
            // Check if non-admin user has access to this project
            if (!isAdmin)
            {
                var hasAccess = await _context.ProjectAssignments
                    .AnyAsync(pa => pa.ProjectId == id && pa.UserId == currentUser.Id);

                if (!hasAccess)
                {
                    return Forbid();
                }
            }

            // Get existing summaries for this project
            var existingSummaries = await _context.MonthlySummaries
                .Where(ms => ms.ProjectId == id)
                .Include(ms => ms.User)
                .OrderByDescending(ms => ms.Year)
                .ThenByDescending(ms => ms.Month)
                .ToListAsync();

            var viewModel = new MonthlySummaryViewModel
            {
                ProjectId = id.Value,
                ProjectName = project.Name,
                Year = DateTime.Now.Year,
                Month = DateTime.Now.Month,
                ExistingSummaries = existingSummaries
            };

            return View(viewModel);
        }

        // POST: Worklog/GenerateSummary
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateSummary(MonthlySummaryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(MonthlySummary), new { id = model.ProjectId });
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
            
            // Check if non-admin user has access to this project
            if (!isAdmin)
            {
                var hasAccess = await _context.ProjectAssignments
                    .AnyAsync(pa => pa.ProjectId == model.ProjectId && pa.UserId == currentUser.Id) ||
                    await _context.ProjectShadowResourceAssignments
                    .AnyAsync(psa => psa.ProjectId == model.ProjectId && psa.ShadowResourceId == currentUser.Id);

                if (!hasAccess)
                {
                    return Forbid();
                }
            }

            // Get all team members for this project (regular assignments)
            var projectMemberIds = await _context.ProjectAssignments
                .Where(pa => pa.ProjectId == model.ProjectId)
                .Select(pa => pa.UserId)
                .Distinct()
                .ToListAsync();
                
            // Add shadow resources if requested
            if (model.IncludeShadowResources)
            {
                var shadowResourceIds = await _context.ProjectShadowResourceAssignments
                    .Where(psa => psa.ProjectId == model.ProjectId)
                    .Select(psa => psa.ShadowResourceId)
                    .Distinct()
                    .ToListAsync();
                    
                projectMemberIds.AddRange(shadowResourceIds);
                projectMemberIds = projectMemberIds.Distinct().ToList();
            }

            // Get the first and last day of the month
            var startDate = new DateTime(model.Year, model.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Delete existing summaries for this month if they exist
            var existingSummaries = await _context.MonthlySummaries
                .Where(ms => ms.ProjectId == model.ProjectId && 
                           ms.Year == model.Year && 
                           ms.Month == model.Month)
                .ToListAsync();
            
            if (existingSummaries.Any())
            {
                _context.MonthlySummaries.RemoveRange(existingSummaries);
                await _context.SaveChangesAsync();
            }

            // Generate summary for each team member
            foreach (var userId in projectMemberIds)
            {
                // Get worklogs for this user in the specified month
                var directWorklogs = await _context.Worklogs
                    .Where(w => w.ProjectId == model.ProjectId && 
                              w.UserId == userId && 
                              !w.IsShadowResourceWorklog &&
                              w.Date >= startDate && 
                              w.Date <= endDate)
                    .ToListAsync();
                
                // Get shadow resource worklogs if this user is a shadow resource
                var shadowWorklogs = model.IncludeShadowResources 
                    ? await _context.Worklogs
                        .Where(w => w.ProjectId == model.ProjectId &&
                                  w.ShadowResourceId == userId &&
                                  w.IsShadowResourceWorklog &&
                                  w.Date >= startDate &&
                                  w.Date <= endDate)
                        .ToListAsync()
                    : new List<Worklog>();
                
                // Combine both types of worklogs
                var userWorklogs = directWorklogs.Concat(shadowWorklogs).ToList();
                
                if (userWorklogs.Any())
                {
                    var totalHours = userWorklogs.Sum(w => w.HoursWorked);
                    
                    // Generate summary text
                    var summary = $"Total hours worked: {totalHours}\n\n";
                    
                    // Add breakdown of direct vs shadow work if applicable
                    if (directWorklogs.Any() && shadowWorklogs.Any())
                    {
                        var directHours = directWorklogs.Sum(w => w.HoursWorked);
                        var shadowHours = shadowWorklogs.Sum(w => w.HoursWorked);
                        summary += $"Direct work: {directHours} hours\nShadow resource work: {shadowHours} hours\n\n";
                    }
                    
                    // Group worklogs by task type and add to summary
                    var taskTypes = userWorklogs
                        .Where(w => !string.IsNullOrEmpty(w.TaskType))
                        .GroupBy(w => w.TaskType)
                        .Select(g => new { 
                            TaskType = g.Key, 
                            Hours = g.Sum(w => w.HoursWorked),
                            Count = g.Count()
                        });
                    
                    if (taskTypes.Any())
                    {
                        summary += "Task breakdown:\n";
                        foreach (var task in taskTypes)
                        {
                            summary += $"- {task.TaskType}: {task.Hours} hours ({task.Count} entries)\n";
                        }
                    }

                    // Create the monthly summary
                    var monthlySummary = new MonthlySummary
                    {
                        ProjectId = model.ProjectId,
                        UserId = userId,
                        Year = model.Year,
                        Month = model.Month,
                        TotalHours = totalHours,
                        Summary = summary,
                        GeneratedAt = DateTime.Now,
                        IncludesShadowWork = model.IncludeShadowResources
                    };

                    _context.Add(monthlySummary);
                }
            }

            await _context.SaveChangesAsync();
            
            return RedirectToAction(nameof(MonthlySummary), new { id = model.ProjectId });
        }
    }
} 