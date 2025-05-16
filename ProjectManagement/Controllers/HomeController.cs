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

            // Get dashboard data
            var viewModel = new DashboardViewModel();

            if (isAdmin)
            {
                // Admin dashboard data
                viewModel.TotalProjects = await _context.Projects.CountAsync();
                viewModel.TotalEmployees = await _userManager.GetUsersInRoleAsync("Employee")
                    .ContinueWith(t => t.Result.Count);
                viewModel.ActiveProjects = await _context.Projects
                    .Where(p => p.Status == "In Progress")
                    .CountAsync();
                viewModel.TotalWorklogs = await _context.Worklogs.CountAsync();

                // Recent projects
                viewModel.RecentProjects = await _context.Projects
                    .OrderByDescending(p => p.Id)
                    .Take(5)
                    .ToListAsync();

                // Recent worklogs
                viewModel.RecentWorklogs = await _context.Worklogs
                    .Include(w => w.Project)
                    .Include(w => w.User)
                    .OrderByDescending(w => w.Date)
                    .Take(5)
                    .ToListAsync();
            }
            else
            {
                // Employee dashboard data
                viewModel.TotalProjects = await _context.ProjectAssignments
                    .Where(pa => pa.UserId == currentUser.Id)
                    .Select(pa => pa.ProjectId)
                    .Distinct()
                    .CountAsync() +
                    await _context.ProjectShadowResourceAssignments
                    .Where(psa => psa.ShadowResourceId == currentUser.Id)
                    .Select(psa => psa.ProjectId)
                    .Distinct()
                    .CountAsync();

                viewModel.ActiveProjects = await _context.ProjectAssignments
                    .Where(pa => pa.UserId == currentUser.Id)
                    .Join(_context.Projects.Where(p => p.Status == "In Progress"),
                        pa => pa.ProjectId,
                        p => p.Id,
                        (pa, p) => p.Id)
                    .Distinct()
                    .CountAsync() +
                    await _context.ProjectShadowResourceAssignments
                    .Where(psa => psa.ShadowResourceId == currentUser.Id)
                    .Join(_context.Projects.Where(p => p.Status == "In Progress"),
                        psa => psa.ProjectId,
                        p => p.Id,
                        (psa, p) => p.Id)
                    .Distinct()
                    .CountAsync();

                viewModel.TotalWorklogs = await _context.Worklogs
                    .Where(w => w.UserId == currentUser.Id)
                    .CountAsync();

                // Calculate total hours logged
                var allUserWorklogs = await _context.Worklogs
                    .Where(w => w.UserId == currentUser.Id)
                    .ToListAsync();
                
                viewModel.TotalHours = allUserWorklogs.Sum(w => w.HoursWorked);

                // User's projects (both regular and shadow resource assignments)
                var regularProjects = await _context.ProjectAssignments
                    .Where(pa => pa.UserId == currentUser.Id)
                    .Include(pa => pa.Project)
                    .Select(pa => pa.Project)
                    .Distinct()
                    .ToListAsync();

                var shadowProjects = await _context.ProjectShadowResourceAssignments
                    .Where(psa => psa.ShadowResourceId == currentUser.Id)
                    .Include(psa => psa.Project)
                    .Select(psa => psa.Project)
                    .Distinct()
                    .ToListAsync();

                viewModel.UserProjects = regularProjects
                    .Union(shadowProjects, new ProjectComparer())
                    .OrderBy(p => p.Name)
                    .ToList();

                // Recent worklogs
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
