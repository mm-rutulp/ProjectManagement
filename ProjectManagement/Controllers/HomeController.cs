using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Data;
using ProjectManagement.Models;
using ProjectManagement.ViewModels;
using System.Diagnostics;

namespace ProjectManagement.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Index()
        {
            if (!_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            
            var viewModel = new DashboardViewModel();

            if (isAdmin)
            {
                
                viewModel.TotalProjects = await _context.Projects.CountAsync();

                viewModel.TotalEmployees = await _userManager.GetUsersInRoleAsync("Employee")
                    .ContinueWith(t => t.Result.Count);

                viewModel.ActiveProjects = await _context.Projects
                    .Where(p => p.Status == "In Progress")
                    .CountAsync();
                viewModel.TotalWorklogs = await _context.Worklogs.CountAsync();
              
                viewModel.RecentProjects = await _context.Projects
                    .Where(p => !p.IsDeleted)
                    .OrderByDescending(p => p.Id)
                    .Take(5)
                    .ToListAsync();

                viewModel.RecentWorklogs = await _context.Worklogs
                    .Include(w => w.Project)
                    .Include(w => w.User)
                    .OrderByDescending(w => w.Date)
                    .Where(w => !w.IsDeleted)
                    .Take(5)
                    .ToListAsync();
            }
            else
            {
              
                viewModel.TotalProjects = await _context.ProjectAssignments
                    .Where(pa => pa.UserId == currentUser.Id && !pa.IsDeleted)
                    .Select(pa => pa.ProjectId)
                    .Distinct()
                    .CountAsync() +
                    await _context.ProjectShadowResourceAssignments
                    .Where(psa => psa.ShadowResourceId == currentUser.Id && !psa.IsDeleted)
                    .Select(psa => psa.ProjectId)
                    .Distinct()
                    .CountAsync();

                viewModel.ActiveProjects = await _context.ProjectAssignments
                    .Where(pa => pa.UserId == currentUser.Id && !pa.IsDeleted)
                    .Join(_context.Projects.Where(p => p.Status == "In Progress"),
                        pa => pa.ProjectId,
                        p => p.Id,
                        (pa, p) => p.Id)
                    .Distinct()
                    .CountAsync() +
                    await _context.ProjectShadowResourceAssignments
                    .Where(psa => psa.ShadowResourceId == currentUser.Id && !psa.IsDeleted)
                    .Join(_context.Projects.Where(p => p.Status == "In Progress"),
                        psa => psa.ProjectId,
                        p => p.Id,
                        (psa, p) => p.Id)
                    .Distinct()
                    .CountAsync();

                viewModel.TotalWorklogs = await _context.Worklogs
                    .Where(w => w.UserId == currentUser.Id)
                    .CountAsync();


                var currentUserId = currentUser.Id;

                // Date filter — first day of current month
                var currentTime = DateTime.UtcNow;
                var firstDayOfMonth = new DateTime(currentTime.Year, currentTime.Month, 1);

                // Check if user is a regular employee
                bool isRegular = await _context.ProjectAssignments
                    .AnyAsync(pa => pa.UserId == currentUserId && !pa.IsDeleted);

                // Check if user is a shadow resource
                bool isShadow = await _context.ProjectShadowResourceAssignments
                    .AnyAsync(psa => psa.ShadowResourceId == currentUserId && !psa.IsDeleted);

                decimal totalHours = 0;

                if (isRegular && isShadow)
                {
                    // Mixed role — worklogs as user and as shadow
                    var logsAsUser = await _context.Worklogs
                        .Where(w => w.UserId == currentUserId && !w.IsDeleted && w.Date >= firstDayOfMonth)
                        .ToListAsync();

                    var logsAsShadow = await _context.Worklogs
                        .Where(w => w.IsShadowResourceWorklog && w.ShadowResourceId == currentUserId && !w.IsDeleted && w.Date >= firstDayOfMonth)
                        .ToListAsync();

                    totalHours = logsAsUser.Sum(w => w.HoursWorked) + logsAsShadow.Sum(w => w.HoursWorked);
                }
                else if (isRegular)
                {
                    var logsAsUser = await _context.Worklogs
                        .Where(w => w.UserId == currentUserId && !w.IsDeleted && w.Date >= firstDayOfMonth)
                        .ToListAsync();

                    totalHours = logsAsUser.Sum(w => w.HoursWorked);
                }
                else if (isShadow)
                {
                    var logsAsShadow = await _context.Worklogs
                        .Where(w => w.IsShadowResourceWorklog && w.ShadowResourceId == currentUserId && !w.IsDeleted && w.Date >= firstDayOfMonth)
                        .ToListAsync();

                    totalHours = logsAsShadow.Sum(w => w.HoursWorked);
                }

                viewModel.TotalHours = totalHours;


                /*   var currentUserId = currentUser.Id;


                    var currentTime = DateTime.UtcNow;
                    var firstDayOfMonth = new DateTime(currentTime.Year, currentTime.Month, 1);

                    // Projects as regular employee
                    var regularProjectIds = await _context.ProjectAssignments
                        .Where(pa => pa.UserId == currentUserId && !pa.IsDeleted)
                        .Select(pa => pa.ProjectId)
                        .Distinct()
                        .ToListAsync();

                    // Projects as shadow resource
                    var shadowProjectIds = await _context.ProjectShadowResourceAssignments
                        .Where(psa => psa.ShadowResourceId == currentUserId && !psa.IsDeleted)
                        .Select(psa => psa.ProjectId)
                        .Distinct()
                        .ToListAsync();


                    var assignedUserProjectId = _context.ProjectAssignments.Where(x => x.UserId == currentUserId).Select(x => x.ProjectId).ToList();

                    decimal totalHours = _context.Worklogs.Where(x => assignedUserProjectId.Contains(x.ProjectId) && x.UserId == currentUserId).Sum(x => x.HoursWorked);


                    var shadowResourceProjects = _context.ProjectShadowResourceAssignments
                        .Where(x => x.ShadowResourceId == currentUserId)
                        .ToDictionary(x => x.ProjectId, x => x.ProjectOnBoardUserId);

                    totalHours += (from Worklog in _context.Worklogs
                                   join shadowResource in shadowResourceProjects
                                                         on new { Worklog.ProjectId, Worklog.UserId }
                                                        equals new { ProjectId = shadowResource.Key, UserId = shadowResource.Value }
                                   select Worklog.HoursWorked).Sum();

                    */
                //decimal totalHours = await _context.Worklogs
                //    .Where(w =>
                //        !w.IsDeleted &&
                //        w.Date >= firstDayOfMonth &&
                //        (
                //            // Regular: user logs their own hours
                //            (regularProjectIds.Contains(w.ProjectId) && w.UserId == currentUserId && !w.IsShadowResourceWorklog) ||

                //            // Shadow submitted work on behalf of this user
                //            (regularProjectIds.Contains(w.ProjectId) && w.UserId == currentUserId && w.IsShadowResourceWorklog) ||

                //            // This user is a shadow resource logging for others
                //            (shadowProjectIds.Contains(w.ProjectId) && w.ShadowResourceId == currentUserId && w.IsShadowResourceWorklog)
                //        )
                //    )
                //    .SumAsync(w => (decimal?)w.HoursWorked) ?? 0;

                viewModel.TotalHours = totalHours;




                var regularProjects = await _context.ProjectAssignments
                    .Where(pa => pa.UserId == currentUser.Id && !pa.IsDeleted)
                    .Include(pa => pa.Project)
                    .Select(pa => pa.Project)
                    .Distinct()
                    .ToListAsync();

                var shadowProjects = await _context.ProjectShadowResourceAssignments
                    .Where(psa => psa.ShadowResourceId == currentUser.Id && !psa.IsDeleted)
                    .Include(psa => psa.Project)
                    .Select(psa => psa.Project)
                    .Distinct()
                    .ToListAsync();

                viewModel.UserProjects = regularProjects
                    .Union(shadowProjects, new ProjectComparer())
                    .OrderBy(p => p.Name)
                    .ToList();

              
                viewModel.RecentWorklogs = await _context.Worklogs
                    .Where(w => w.UserId == currentUser.Id)
                    .Include(w => w.Project)
                    .OrderByDescending(w => w.Date)
                    .Where(w => !w.IsDeleted)
                    .Take(5)
                    .ToListAsync();
            }

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
