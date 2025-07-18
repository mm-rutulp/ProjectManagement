using ClosedXML.Excel;
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
        // GET: Worklog
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Challenge();
            }

            var userWorklogs = await _context.Worklogs
                .Where(w => w.UserId == currentUser.Id && !w.IsDeleted)
                .Include(w => w.Project)
                .OrderByDescending(w => w.Date)
                .ToListAsync();

            return View(userWorklogs);
        }

        //get detailed worklog based on id 



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
                .Where(pa => pa.ProjectId == id && !pa.User.IsDeleted)
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
       
        [HttpGet]
        public async Task<IActionResult> GetWorklogs(int id, string? userId, DateTime? startDate, DateTime? endDate, int page = 1, int pageSize = 10)
        {
            var query = _context.Worklogs
                .Where(w => w.ProjectId == id && !w.IsDeleted)
                .Include(w => w.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(w => w.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(w => w.Date >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(w => w.Date <= endDate.Value);

            var groupedData = await query
                .GroupBy(w => new { w.UserId, w.User.FullName, w.Date })
                .Select(g => new
                {
                    date = g.Key.Date,
                    userFullName = g.Key.FullName,
                    userId = g.Key.UserId,
                    totalHours = g.Sum(x => x.HoursWorked),
                   

                })
                .OrderByDescending(g => g.date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var totalCount = await query
                .GroupBy(w => new { w.User.FullName, w.Date })
                .CountAsync();

            return Json(new
            {
                data = groupedData,
                totalCount,
                page,
                pageSize
            });
        }

        [HttpGet]
        public async Task<IActionResult> DownloadWorklogsExcel(int id, string? userId, DateTime? startDate, DateTime? endDate)
        {
            return await GenerateWorklogsExcelAsync(id, userId, startDate, endDate);
        }
        //function to download excel report 
        private async Task<FileResult> GenerateWorklogsExcelAsync(int id, string? userId, DateTime? startDate, DateTime? endDate)
        {
            var query = _context.Worklogs
                .Where(w => w.ProjectId == id && !w.IsDeleted)
                .Include(w => w.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(w => w.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(w => w.Date >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(w => w.Date <= endDate.Value);

            var worklogs = await query.OrderByDescending(w => w.Date).ToListAsync();

            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Worklogs");

            // Header
            var headerRow = worksheet.Range("A1:F1");
            worksheet.Cell(1, 1).Value = "Date";
            worksheet.Cell(1, 2).Value = "User";
            worksheet.Cell(1, 3).Value = "Task Type";
            worksheet.Cell(1, 4).Value = "Description";
            worksheet.Cell(1, 5).Value = "Hours Worked";
            worksheet.Cell(1, 6).Value = "Status";

            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightGreen;
            int row = 2;
            foreach (var w in worklogs)
            {
                worksheet.Cell(row, 1).Value = w.Date.ToString("yyyy-MM-dd");
                worksheet.Cell(row, 2).Value = w.User.FullName;
                worksheet.Cell(row, 3).Value = w.TaskType;
                worksheet.Cell(row, 4).Value = w.Description;
                worksheet.Cell(row, 5).Value = w.HoursWorked;
                worksheet.Cell(row, 6).Value = w.Status;
                row++;
            }

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"Worklogs_{DateTime.Now:yyyyMMddHHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }

        [HttpGet]
        public async Task<IActionResult> GetWorklogDetails(int projectId, string userName, DateTime date)
        {
            var logs = await _context.Worklogs
                .Where(w => w.ProjectId == projectId &&
                            !w.IsDeleted &&
                            w.User.FullName == userName &&
                            w.Date == date)
                .Include(w => w.User)
                .Select(w => new
                {
                    worklogId = w.Id,
                    date = w.Date,
                    userFullName = w.User.FullName,
                    hoursWorked = w.HoursWorked,
                    status= w.Status,
                    taskType = w.TaskType,
                    description = w.Description,
                   
                })
                .ToListAsync();

            return Json(logs);
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectWorklogs(int projectId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            if (!isAdmin)
            {
                var hasAccess = await _context.ProjectAssignments
                    .AnyAsync(pa => pa.ProjectId == projectId && pa.UserId == currentUser.Id) ||
                    await _context.ProjectShadowResourceAssignments
                    .AnyAsync(psa => psa.ProjectId == projectId && psa.ShadowResourceId == currentUser.Id);

                if (!hasAccess)
                {
                    return Forbid();
                }
            }

            var worklogs = await _context.Worklogs
                .Where(w => w.ProjectId == projectId)
                .Include(w => w.User)
                .OrderByDescending(w => w.Date)
                .Select(w => new {
                    w.Id,
                    Date = w.Date.ToString("yyyy-MM-dd"),
                    User = w.User.FullName,
                    w.HoursWorked,
                    w.TaskType,
                    w.Status,
                    w.Description
                })
                .ToListAsync();

            return Json(worklogs);
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
                UserId = currentUser.Id, // Default to current user
                Status ="Pending"
            };

            // If the user is a shadow resource for this project, populate the dropdown with users who added them
            if (shadowResourceAssignments.Any())
            {
                viewModel.AvailableUsers = shadowResourceAssignments.Select(psa => psa.ProjectOnBoardUser).Distinct().ToList();
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
                    Status= model.Status,
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
         
            var isShadowForOwner = await _context.ProjectShadowResourceAssignments
                .AnyAsync(psa =>
                   psa.ProjectId == worklog.ProjectId &&
                   psa.ShadowResourceId == currentUser.Id &&
                   psa.ProjectOnBoardUserId == worklog.UserId &&
                   !psa.IsDeleted);

            // Only the owner or admin can edit a worklog
            if ( worklog.UserId != currentUser.Id && !isShadowForOwner)
            {
                TempData["UnauthorizedMessage"] = "You are not authorized to edit this worklog.";
                return RedirectToAction("Project", "Worklog", new { id = worklog.ProjectId });



            }
            var shadowResourceAssignments = await _context.ProjectShadowResourceAssignments
                .Where(psa => psa.ProjectId == worklog.ProjectId && psa.ShadowResourceId == currentUser.Id)
                .Include(psa => psa.ProjectOnBoardUser)
                .ToListAsync();

            var availableUsers = shadowResourceAssignments.Any()
                ? shadowResourceAssignments.Select(psa => psa.ProjectOnBoardUser).Distinct().ToList()
                : new List<ApplicationUser> { currentUser };

            var viewModel = new WorklogCreateViewModel
            {
                WorklogId =worklog.Id,
                
                ProjectId = worklog.ProjectId,
                ProjectName = worklog.Project.Name,
                Date = worklog.Date,
                HoursWorked = worklog.HoursWorked,
                Description = worklog.Description,
                TaskType = worklog.TaskType,
                AvailableUsers = availableUsers,
                Status = worklog.Status
            };

            return View("Create",viewModel);
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
            var isShadowResource = await _context.ProjectShadowResourceAssignments
            .AnyAsync(psa =>
                psa.ProjectId == worklog.ProjectId &&
                psa.ShadowResourceId == currentUser.Id &&
                !psa.IsDeleted);
            // Only the owner or admin can edit a worklog
            if (!isAdmin && worklog.UserId != currentUser.Id && !isShadowResource)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                worklog.Date = model.Date;
                worklog.HoursWorked = model.HoursWorked;
                worklog.Description = model.Description;
                worklog.TaskType = model.TaskType;
                worklog.Status = model.Status;
                worklog.UpdatedAt = DateTime.Now;

                _context.Update(worklog);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Project), new { id = worklog.ProjectId });
            }

            return View(model);
        }

        // GET: Worklog/Delete/5
        [HttpGet]
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

            bool isShadowResource = await _context.ProjectShadowResourceAssignments.AnyAsync(psa =>
                 psa.ProjectId == worklog.ProjectId &&
                 psa.ShadowResourceId == currentUser.Id &&
                 psa.ProjectOnBoardUserId == worklog.UserId &&
                !psa.IsDeleted
            );

            int projectId = worklog.ProjectId;
            // Only the owner or admin can delete a worklog
            if (!isAdmin && worklog.UserId != currentUser.Id && !isShadowResource)
            {
                TempData["UnauthorizedMessage"] = "You are not authorized to delete this worklog.";
                return RedirectToAction(nameof(Project), new { id = projectId });
            }

            worklog.IsDeleted = true;
            _context.Worklogs.Update(worklog);
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
        
        // GET: Worklog/CreateBulk/5
        [HttpGet]
        public async Task<IActionResult> CreateBulk(int? id)
        {
            if (id == null)
                return NotFound();

            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);

            // Check project access
            var hasAccess = await _context.ProjectAssignments
                .AnyAsync(pa => pa.ProjectId == id && pa.UserId == currentUser.Id) ||
                await _context.ProjectShadowResourceAssignments
                .AnyAsync(psa => psa.ProjectId == id && psa.ShadowResourceId == currentUser.Id);

            if (!hasAccess)
                return Forbid();

            var shadowAssignments = await _context.ProjectShadowResourceAssignments
                .Where(psa => psa.ProjectId == id && psa.ShadowResourceId == currentUser.Id)
                .Include(psa => psa.ProjectOnBoardUser)
                .ToListAsync();

            var viewModel = new WorklogBulkCreateViewModel
            {
                ProjectId = id.Value,
                ProjectName = project.Name,
                UserId = currentUser.Id,
                Date = DateTime.Today,
                AvailableUsers = shadowAssignments.Any()
                    ? shadowAssignments.Select(psa => psa.ProjectOnBoardUser).Distinct().ToList()
                    : new List<ApplicationUser> { currentUser },
                Worklogs = new List<WorklogRowInput> { new WorklogRowInput() } // at least one row
            };

            return View(viewModel);
        }

        // POST: Worklog/CreateBulk/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBulk(WorklogBulkCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var currentUser = await _userManager.GetUserAsync(User);

            var hasAccess = await _context.ProjectAssignments
                .AnyAsync(pa => pa.ProjectId == model.ProjectId && pa.UserId == currentUser.Id) ||
                await _context.ProjectShadowResourceAssignments
                .AnyAsync(psa => psa.ProjectId == model.ProjectId && psa.ShadowResourceId == currentUser.Id);

            if (!hasAccess)
            {
                return Forbid();
            }

            var isValidUser = await _context.ProjectShadowResourceAssignments
                .AnyAsync(psa => psa.ProjectId == model.ProjectId &&
                                 psa.ShadowResourceId == currentUser.Id &&
                                 psa.ProjectOnBoardUserId == model.UserId);

            if (!isValidUser && model.UserId != currentUser.Id)
            {
                ModelState.AddModelError("UserId", "Invalid user selection");
                return View(model);
            }

            var worklogsToAdd = model.Worklogs.Select(w => new Worklog
            {
                ProjectId = model.ProjectId,
                UserId = model.UserId,
                Date = model.Date,
                HoursWorked = w.HoursWorked,
                Description = w.Description,
                TaskType = w.TaskType,
                Status = w.Status,
                CreatedAt = DateTime.UtcNow,
                IsShadowResourceWorklog = model.UserId != currentUser.Id,
                ShadowResourceId = model.UserId != currentUser.Id ? currentUser.Id : null
            }).ToList();

            _context.Worklogs.AddRange(worklogsToAdd);
            await _context.SaveChangesAsync();

            return RedirectToAction("Project", "Worklog", new { id = model.ProjectId });
        }

        // GET: Worklog/EditBulkWorklogs/projectId=7&userId=uid&date=2025-07-09T00:00:005
        [HttpGet]
        public async Task<IActionResult> EditBulkWorklogs(int projectId, string userId, DateTime date)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser.Id;
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            var worklogs = await _context.Worklogs
                .Where(w => w.ProjectId == projectId && w.UserId == userId && w.Date == date && !w.IsDeleted)
                .ToListAsync();

            if (!worklogs.Any())
            {
                TempData["UnauthorizedMessage"] = "No worklogs found for this date.";
                return RedirectToAction("Project", new { id = projectId });
            }

            var isShadowResource = await _context.ProjectShadowResourceAssignments.AnyAsync(psa =>
                psa.ProjectId == projectId &&
                psa.ShadowResourceId == currentUserId &&
                psa.ProjectOnBoardUserId == userId &&
                !psa.IsDeleted);

            // Authorization
            if (!isAdmin && currentUserId != userId && !isShadowResource)
            {
                TempData["UnauthorizedMessage"] = "You are not authorized to edit these worklogs.";
                return RedirectToAction("Project", new { id = projectId });
            }

            var shadowAssignments = await _context.ProjectShadowResourceAssignments
                .Where(psa => psa.ProjectId == projectId && psa.ShadowResourceId == currentUserId)
                .Include(psa => psa.ProjectOnBoardUser)
                .ToListAsync();

            var availableUsers = shadowAssignments.Any()
                ? shadowAssignments.Select(psa => psa.ProjectOnBoardUser).Distinct().ToList()
                : new List<ApplicationUser> { currentUser };

            var viewModel = new WorklogBulkCreateViewModel
            {
                ProjectId = projectId,
                ProjectName = project.Name,
                UserId = userId,
                Date = date,
                AvailableUsers = availableUsers,
                Worklogs = worklogs.Select(w => new WorklogRowInput
                {
                    Id = w.Id,
                    HoursWorked = w.HoursWorked,
                    Description = w.Description,
                    Status = w.Status,
                    TaskType = w.TaskType
                }).ToList()
            };

            return View("CreateBulk", viewModel);
        }

        // POST: Worklog/EditBulkWorklogs/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBulkWorklogs(WorklogBulkCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View("CreateBulk", model);

            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser.Id;
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            foreach (var row in model.Worklogs)
            {
                var worklog = await _context.Worklogs.FindAsync(row.Id);
                if (worklog == null || worklog.IsDeleted || worklog.ProjectId != model.ProjectId)
                    continue;

                // Check if current user is allowed to edit this specific worklog
                bool isOwner = worklog.UserId == currentUserId;
                bool isShadow = worklog.IsShadowResourceWorklog && worklog.ShadowResourceId == currentUserId;

                if (!isAdmin && !isOwner && !isShadow)
                {
                    TempData["UnauthorizedMessage"] = "You are not authorized to edit one or more worklogs.";
                    return RedirectToAction("Index", "Home");
                }

                // Passed checks, allow edit
                worklog.TaskType = row.TaskType;
                worklog.Status = row.Status;
                worklog.HoursWorked = row.HoursWorked;
                worklog.Description = row.Description;
                worklog.UpdatedAt = DateTime.UtcNow;

                _context.Update(worklog);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Project", new { id = model.ProjectId });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFromBulk(int id, long projectId, string userId, DateTime date)
        {
            var worklog = await _context.Worklogs.FindAsync(id);
            if (worklog == null)
            {
                TempData["Error"] = "Worklog not found.";
                return RedirectToAction("EditBulkWorklogs", new { projectId, userId, date });
            }

            // Soft delete (recommended)
            worklog.IsDeleted = true;
            _context.Worklogs.Update(worklog);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Worklog deleted successfully.";

            return RedirectToAction("EditBulkWorklogs", new { projectId, userId, date });
        }

    }
} 