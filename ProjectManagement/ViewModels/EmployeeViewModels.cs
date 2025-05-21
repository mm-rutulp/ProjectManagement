using ProjectManagement.Models;
using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.ViewModels
{
    public class EmployeeDetailsViewModel
    {
        public ApplicationUser Employee { get; set; } = null!;
        public ICollection<ProjectAssignment> ProjectAssignments { get; set; } = new List<ProjectAssignment>();
        public ICollection<ProjectShadowResourceAssignment> ShadowResourceAssignments { get; set; } = new List<ProjectShadowResourceAssignment>();
        public ICollection<Worklog> RecentWorklogs { get; set; } = new List<Worklog>();
    }

    public class EmployeeEditViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Department")]
        public string? Department { get; set; }

        [Display(Name = "Position")]
        public string? Position { get; set; }
    }

    public class ResetPasswordViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
} 