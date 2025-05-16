using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Data;
using ProjectManagement.Models;
using ProjectManagement.ViewModels;

namespace ProjectManagement.Controllers
{
    [Authorize(Roles = "Admin")]
    public class EmployeeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EmployeeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Employee
        public async Task<IActionResult> Index()
        {
            var employees = await _userManager.GetUsersInRoleAsync("Employee");
            return View(employees);
        }

        // GET: Employee/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _userManager.FindByIdAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            // Get projects assigned to this employee
            var assignedProjects = await _context.ProjectAssignments
                .Where(pa => pa.UserId == id)
                .Include(pa => pa.Project)
                .OrderBy(pa => pa.Project.Name)
                .ToListAsync();
                
            // Get shadow resource assignments for this employee
            var shadowAssignments = await _context.ProjectShadowResourceAssignments
                .Where(psa => psa.ShadowResourceId == id)
                .Include(psa => psa.Project)
                .Include(psa => psa.ProjectOnBoardUser)
                .OrderBy(psa => psa.Project.Name)
                .ToListAsync();

            // Get recent worklogs from this employee (both direct and as shadow resource)
            var recentWorklogs = await _context.Worklogs
                .Where(w => w.UserId == id || (w.ShadowResourceId == id && w.IsShadowResourceWorklog))
                .Include(w => w.Project)
                .OrderByDescending(w => w.Date)
                .Take(10)
                .ToListAsync();

            var viewModel = new EmployeeDetailsViewModel
            {
                Employee = employee,
                ProjectAssignments = assignedProjects,
                ShadowResourceAssignments = shadowAssignments,
                RecentWorklogs = recentWorklogs
            };

            return View(viewModel);
        }

        // GET: Employee/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _userManager.FindByIdAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            var viewModel = new EmployeeEditViewModel
            {
                Id = employee.Id,
                Email = employee.Email,
                FullName = employee.FullName,
                Department = employee.Department,
                Position = employee.Position
            };

            return View(viewModel);
        }

        // POST: Employee/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EmployeeEditViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var employee = await _userManager.FindByIdAsync(id);
                if (employee == null)
                {
                    return NotFound();
                }

                employee.FullName = model.FullName;
                employee.Department = model.Department;
                employee.Position = model.Position;

                // Only update email if it has changed
                if (employee.Email != model.Email)
                {
                    // This will also update the UserName since ASP.NET Identity uses email as the username by default
                    var setEmailResult = await _userManager.SetEmailAsync(employee, model.Email);
                    if (!setEmailResult.Succeeded)
                    {
                        foreach (var error in setEmailResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return View(model);
                    }

                    var setUserNameResult = await _userManager.SetUserNameAsync(employee, model.Email);
                    if (!setUserNameResult.Succeeded)
                    {
                        foreach (var error in setUserNameResult.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        return View(model);
                    }
                }

                var result = await _userManager.UpdateAsync(employee);
                if (result.Succeeded)
                {
                    return RedirectToAction(nameof(Index));
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View(model);
        }

        // GET: Employee/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _userManager.FindByIdAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: Employee/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var employee = await _userManager.FindByIdAsync(id);
            if (employee == null)
            {
                return NotFound();
            }

            // First remove any project assignments
            var assignments = await _context.ProjectAssignments
                .Where(pa => pa.UserId == id)
                .ToListAsync();
            
            _context.ProjectAssignments.RemoveRange(assignments);
            
            // Remove any shadow resource assignments where this user is the shadow resource
            var shadowAssignments = await _context.ProjectShadowResourceAssignments
                .Where(psa => psa.ShadowResourceId == id)
                .ToListAsync();
                
            _context.ProjectShadowResourceAssignments.RemoveRange(shadowAssignments);
            
            // Remove any shadow resource assignments where this user added the shadow resource
            var onboardedShadowAssignments = await _context.ProjectShadowResourceAssignments
                .Where(psa => psa.ProjectOnBoardUserId == id)
                .ToListAsync();
                
            _context.ProjectShadowResourceAssignments.RemoveRange(onboardedShadowAssignments);
            
            // Update any worklogs with this shadow resource ID to clear the association
            var shadowWorklogs = await _context.Worklogs
                .Where(w => w.ShadowResourceId == id)
                .ToListAsync();
                
            foreach (var worklog in shadowWorklogs)
            {
                worklog.ShadowResourceId = null;
                worklog.IsShadowResourceWorklog = false;
            }
            
            await _context.SaveChangesAsync();

            // Then delete the user
            var result = await _userManager.DeleteAsync(employee);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(employee);
            }

            return RedirectToAction(nameof(Index));
        }
    }
} 