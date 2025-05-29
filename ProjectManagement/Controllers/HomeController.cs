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

                /* this will get total worklogs of regula users only
                                 var currenttime = DateTime.UtcNow;
                 var firstDayOfMonth = new DateTime(currenttime.Year, currenttime.Month, 1);

                 var monthlyUserWorklogs = await _context.Worklogs
                     .Where(w => w.UserId == currentUser.Id &&
                                 w.Date >= firstDayOfMonth &&
                                 !w.IsDeleted)
                     .ToListAsync();

                 viewModel.TotalHours = monthlyUserWorklogs.Sum(w => w.HoursWorked);
                 */
                var allUserWorklogs = await _context.Worklogs
                    .Where(w => w.UserId == currentUser.Id)
                    .ToListAsync();
                
                viewModel.TotalHours = allUserWorklogs.Sum(w => w.HoursWorked);

              
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
